using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class GlitchedButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;
    public KMBombInfo Bomb;
    public KMSelectable GlitchedButtonSelectable;
    public GameObject GlitchedButtonCap;
    public TextMesh Text;
    public MeshRenderer TextRenderer;
    public MaskShaderManager MaskShaderManager;
    public MeshRenderer Mask;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;

    private string _cyclingBits;
    private int _highlightedBit;
    private int _seqIx;
    private int _flippedBit;
    private int _holdIx;
    private bool _isHeld;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        GlitchedButtonSelectable.OnInteract += GlitchedButtonPress;
        GlitchedButtonSelectable.OnInteractEnded += GlitchedButtonRelease;

        var fontTexture = TextRenderer.sharedMaterial.mainTexture;
        var mr = MaskShaderManager.MakeMaterials();
        TextRenderer.material = mr.Text;
        TextRenderer.material.mainTexture = fontTexture;
        Mask.sharedMaterial = mr.Mask;

        // RULE SEED STARTS HERE
        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat("[The Glitched Button #{0}] Using rule seed: {1}.", _moduleId, rnd.Seed);

        const int bitLength = 16;
        var results = new List<int>();
        while (results.Count < 16)
        {
            var all = Enumerable.Range(0, 1 << bitLength)
                .Where(v =>
                {
                    for (var cycle = 1; cycle < bitLength; cycle++)
                    {
                        var cycled = ((v << cycle) & ((1 << bitLength) - 1)) | (v >> (bitLength - cycle));
                        var nb = countBits(cycled ^ v);
                        if (nb == 0 || nb == 2)
                            return false;
                    }
                    return true;
                })
                .ToList();
            results.Clear();
            while (all.Count > 0 && results.Count < 16)
            {
                var rndIx = rnd.Next(0, all.Count);
                var bits = all[rndIx];
                for (var cycle = 0; cycle < bitLength; cycle++)
                {
                    var compare = ((bits << cycle) & ((1 << bitLength) - 1)) | (bits >> (bitLength - cycle));
                    all.RemoveAll(v => { var nb = countBits(compare ^ v); return nb == 0 || nb == 2; });
                }
                results.Add(bits);
            }
        }
        // END RULE SEED


        _seqIx = Rnd.Range(0, results.Count);
        var seq = results[_seqIx];
        Debug.LogFormat("[The Glitched Button #{0}] Selected bit sequence “{1}” (#{2}).", _moduleId, Convert.ToString(seq, 2).PadLeft(bitLength, '0'), _seqIx + 1);

        _flippedBit = Rnd.Range(0, bitLength);
        _cyclingBits = Convert.ToString(seq ^ (1 << (15 - _flippedBit)), 2).PadLeft(bitLength, '0');
        Debug.LogFormat("[The Glitched Button #{0}] Showing bit sequence “{1}” (flipped bit is #{2}).", _moduleId, _cyclingBits, _flippedBit + 1);

        Debug.LogFormat("[The Glitched Button #{0}] Solution: {1}. () = hold, [] = release.", _moduleId, _cyclingBits
            .Select((ch, ix) => ix == _flippedBit ? string.Format("({0})", ch) : ch.ToString())
            .Select((ch, ix) => ix == _seqIx ? string.Format("[{0}]", ch) : ch)
            .Join(""));

        Text.text = _cyclingBits;
        StartCoroutine(CycleBits());
    }

    private void OnDestroy()
    {
        MaskShaderManager.Clear();
    }

    private IEnumerator CycleBits()
    {
        var isSolved = false;
        var solveStartTime = 0f;
        var fadeDuration = 4.7f;
        var scrollTime = 0.7f;

        _highlightedBit = 8;

        while (!isSolved || (Time.time - solveStartTime) < fadeDuration)
        {
            float time = Time.time;
            Vector3 start = Text.transform.localPosition;
            Vector3 end = Text.transform.localPosition + new Vector3(-.016f, 0f, 0f);
            _highlightedBit = (_highlightedBit + 1) % 16;
            while (time + scrollTime > Time.time)
            {
                Text.transform.localPosition = Vector3.Lerp(start, end, (Time.time - time) / scrollTime);
                var breakIx = (_highlightedBit + 8) % 16;
                var cycled = _cyclingBits.Substring(breakIx) + _cyclingBits.Substring(0, breakIx);
                if (!_moduleSolved)
                {
                    cycled = cycled.Insert(9, "</color>");
                    cycled = cycled.Insert(8, "<color=#9999ff>");
                }
                Text.text = cycled;

                if (isSolved)
                {
                    var v = 1 - ((Time.time - solveStartTime) / fadeDuration);
                    Text.color = new Color(0, v, 0, 1);
                }
                yield return null;
            }
            Text.transform.localPosition = start;

            if (_moduleSolved && !isSolved)
            {
                solveStartTime = Time.time;
                isSolved = true;
            }
        }
    }

    private int countBits(int v)
    {
        var bits = 0;
        while (v > 0)
        {
            bits++;
            v &= v - 1;
        }
        return bits;
    }

    private bool GlitchedButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        _isHeld = true;
        _holdIx = _highlightedBit;
        return false;
    }

    private void GlitchedButtonRelease()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        StartCoroutine(AnimateButton(-0.05f, 0f));
        _isHeld = false;
        if (_moduleSolved)
            return;

        var input = _cyclingBits
            .Select((ch, ix) => ix == _holdIx ? string.Format("({0})", ch) : ch.ToString())
            .Select((ch, ix) => ix == _highlightedBit ? string.Format("[{0}]", ch) : ch)
            .Join("");

        if (_holdIx == _flippedBit && _highlightedBit == _seqIx)
        {
            Debug.LogFormat("[The Glitched Button #{0}] Correct input: {1}. Module solved.",
                _moduleId, input);
            Module.HandlePass();
            _moduleSolved = true;
            Text.color = new Color(0, 1, 0, 1);
        }
        else if (!_isAutosolving)
        {
            Debug.LogFormat("[The Glitched Button #{0}] Incorrect input: {1}. Strike.",
                _moduleId, input);
            Module.HandleStrike();
        }
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        float duration = 0.1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            GlitchedButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        GlitchedButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} hold 00100 [Hold on the last digit of that sequence.] | !{0} release 00111 [Release on the last digit of that sequence.]";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        var m = Regex.Match(command, @"^\s*(?<action>hold|release)\s+(?<bits>[01]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;
        var isHold = m.Groups["action"].Value.EqualsIgnoreCase("hold");
        if (isHold == _isHeld)
        {
            yield return "sendtochaterror " + (_isHeld ? "The button is already being held." : "The button has not been held yet.");
            yield break;
        }
        var inputBits = m.Groups["bits"].Value;
        var str = _cyclingBits + _cyclingBits;
        var p1 = str.IndexOf(inputBits);
        if (p1 == -1)
        {
            yield return string.Format("sendtochaterror {0} is not found in the sequence.", inputBits);
            yield break;
        }
        var p2 = str.IndexOf(inputBits, p1 + 1);
        if (p2 != -1 && p2 % 16 != p1)
        {
            yield return string.Format("sendtochaterror {0} is found in the sequence multiple times.", inputBits);
            yield break;
        }
        yield return null;
        while (_highlightedBit != (p1 + inputBits.Length - 1) % 16)
            yield return "trycancel";
        if (isHold)
            GlitchedButtonSelectable.OnInteract();
        else
            GlitchedButtonSelectable.OnInteractEnded();
    }

    private bool _isAutosolving;

    public IEnumerator TwitchHandleForcedSolve()
    {
        _isAutosolving = true;
        if (_isHeld && _holdIx != _flippedBit)
        {
            // Tank fake strike
            GlitchedButtonSelectable.OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
        }
        if (!_isHeld)
        {
            while (_highlightedBit != _flippedBit)
                yield return true;
            GlitchedButtonSelectable.OnInteract();
        }
        while (_highlightedBit != _seqIx)
            yield return true;
        GlitchedButtonSelectable.OnInteractEnded();
        yield break;
    }
}
