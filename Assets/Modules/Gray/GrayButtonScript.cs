using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GrayButton;
using KModkit;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class GrayButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;
    public KMSelectable GrayButtonSelectable;
    public GameObject GrayButtonCap;
    public MeshRenderer GrayButtonSymbol;
    public TextMesh GrayButtonText;
    public Material[] Symbols;
    public Material LedUnlit;
    public Material LedLit;

    public MeshRenderer[] Leds;
    public Transform ProgressBar;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;
    private int[] _solution;
    private List<float> _solutionSec;
    private readonly List<float> _input = new List<float>();
    private float? _lastHold;
    private Coroutine _timerCoroutine;
    private List<float> _tpInput = null;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;

        // START RULE SEED
        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat("[The Gray Button #{0}] Using rule seed: {1}.", _moduleId, rnd.Seed);
        var mazes = Enumerable.Range(0, 9).Select(mazeId => MazeLayout.Generate(10, 10, rnd)).ToArray();
        // END RULE SEED

        var sn = BombInfo.GetSerialNumber();
        var startPos = convert(sn[2]) % 10 + 10 * (convert(sn[5]) % 10);
        var symbol = Rnd.Range(0, 9);
        var maze = mazes[symbol];
        var results = maze.FindPositionsAtMaxDistance(startPos, 23).PickRandom();
        var dist = results.Distance;
        _solution = new int[3];
        _solution[2] = dist % 4;
        dist /= 4;
        _solution[1] = dist % 3;
        dist /= 3;
        _solution[0] = dist;

        Debug.LogFormat(@"[The Gray Button #{0}] Starting position: ({1}, {2}). Goal position: ({3}, {4}). Distance: {5}.", _moduleId,
            startPos % 10, startPos / 10, results.Cell % 10, results.Cell / 10, results.Distance);

        var ex = new int[] { 1 };
        foreach (var v in _solution)
            ex = ex.Select(n => n >= v + 1 ? n + 1 : n).Concat(new[] { v + 1 }).ToArray();
        _solutionSec = ex.Select(f => f * .1f).ToList();

        Debug.LogFormat(@"[The Gray Button #{0}] Example hold durations to solve: {1} ({2}).", _moduleId, ex.Select(f => f + "s").Join(", "), _solution.Join(", "));

        GrayButtonSymbol.sharedMaterial = Symbols[symbol];
        GrayButtonText.text = string.Format("{0}, {1}", results.Cell % 10, results.Cell / 10);
        GrayButtonSelectable.OnInteract += GrayButtonPress;
        GrayButtonSelectable.OnInteractEnded += GrayButtonRelease;
    }

    private void SetLeds()
    {
        for (var i = 0; i < 3; i++)
            Leds[i].sharedMaterial = _input.Count > i ? LedLit : LedUnlit;
    }

    private static int convert(char ch)
    {
        return ch >= '0' && ch <= '9' ? ch - '0' : ch - 'A' + 10;
    }

    private bool GrayButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        _lastHold = Time.time;
        return false;
    }

    private void GrayButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);

        if (_moduleSolved || _lastHold == null)
            return;

        _input.Add(Time.time - _lastHold.Value);
        SetLeds();

        if (_timerCoroutine != null)
            StopCoroutine(_timerCoroutine);
        ProgressBar.gameObject.SetActive(false);

        if (_input.Count == 4)
        {
            var actualInput = _tpInput ?? _input;
            var holdTimes = actualInput.Select(s => (int) (s * 1000) + "ms").Join(", ");
            var input = Enumerable.Range(1, 3).Select(i => actualInput.Take(i + 1).OrderBy(f => f).IndexOf(f => f == actualInput[i])).ToArray();
            if (!input.SequenceEqual(_solution))
            {
                Debug.LogFormat(@"[The Gray Button #{0}] You held the button for {1}. Strike.", _moduleId, holdTimes);
                Module.HandleStrike();
                _tpInput = null;
            }
            else
            {
                Debug.LogFormat(@"[The Gray Button #{0}] You held the button for {1}. Module solved.", _moduleId, holdTimes);
                Module.HandlePass();
                _moduleSolved = true;
                GrayButtonText.text = "";
            }
            _input.Clear();
            SetLeds();
        }
        else
            _timerCoroutine = StartCoroutine(timer());
    }

    private IEnumerator timer()
    {
        ProgressBar.gameObject.SetActive(true);
        var totalTime = 30f;
        var remainingTime = totalTime;
        while (remainingTime > 0)
        {
            var prop = remainingTime / totalTime;
            ProgressBar.localPosition = new Vector3(0, -.5f + prop / 2, 0);
            ProgressBar.localScale = new Vector3(1, prop, 1);
            yield return null;
            remainingTime -= Time.deltaTime;
        }
        ProgressBar.gameObject.SetActive(false);
        _input.Clear();
        SetLeds();
        _lastHold = null;
        _timerCoroutine = null;
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            GrayButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        GrayButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} hold 2 1 3 4 [hold the button for a series of time intervals of these relative lengths]";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        var m = Regex.Match(command, @"^\s*(?:hold|submit|press)\s+([\d ]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;

        var numbers = m.Groups[1].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (!numbers.All(x => { int tmp; return int.TryParse(x, out tmp); }) || numbers.Length != 4)
        {
            yield return "sendtochaterror Expected exactly 4 integer values.";
            yield break;
        }

        yield return null;
        _tpInput = numbers.Select(f => float.Parse(f) * .1f).ToList();
        for (var i = 0; i < 4; i++)
        {
            GrayButtonSelectable.OnInteract();
            yield return new WaitForSeconds(_tpInput[i]);
            GrayButtonSelectable.OnInteractEnded();
            yield return new WaitForSeconds(.1f);
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        if (_moduleSolved)
            yield break;
        while (_timerCoroutine != null)
            yield return true;

        _tpInput = _solutionSec.ToList();
        for (var i = 0; i < 4; i++)
        {
            GrayButtonSelectable.OnInteract();
            yield return new WaitForSeconds(_tpInput[i] * .1f);
            GrayButtonSelectable.OnInteractEnded();
            yield return new WaitForSeconds(.1f);
        }
    }
}
