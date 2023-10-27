using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class MaroonButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMAudio Audio;
    public KMBombInfo BombInfo;
    public KMSelectable MaroonButtonSelectable;
    public GameObject MaroonButtonCap;
    public MeshRenderer MaroonButtonScreen;
    public Material FlagSelectedScreenMaterial;
    public Material MaroonButtonScreenMaterial;

    public MeshRenderer Mask;
    public MaskShaderManager MaskShaderManager;

    public Transform FlagsParent;
    public Transform SolveParent;
    public Texture[] FlagTextures;
    public Mesh Quad;
    public Light Spotlight;
    public MeshRenderer SolveFlag;
    public Mesh Checkmark;
    public Color CheckmarkHighlightColor;
    public Color CheckmarkColor;

    // Static info
    private static readonly string[] flagNames = { "Algeria", "Angola", "Brazil", "China", "Colombia", "Czech Republic", "Ecuador", "France", "Germany", "Guatemala", "Honduras", "India", "Japan", "Nicaragua", "Panama", "Senegal", "South Korea", "The Gambia", "United Kingdom", "Uruguay" };
    private static readonly int[] flagSizes = { 667, 667, 700, 667, 667, 667, 500, 667, 600, 625, 500, 667, 667, 600, 667, 667, 667, 667, 500, 667 };
    private static readonly string[] continents = { "Africa", "Africa", "South America", "Asia", "South America", "Europe", "South America", "Europe", "Europe", "Central America", "Central America", "Asia", "Asia", "Central America", "Central America", "Africa", "Asia", "Africa", "Europe", "South America" };
    private static readonly float[] flagLatitudes = { 28.0339f, 11.2027f, -14.2350f, 35.8617f, 4.5709f, 49.8175f, -1.8312f, 46.2276f, 51.1657f, 15.7835f, 15.2f, 20.5937f, 36.2048f, 12.8654f, 8.538f, 14.4974f, 35.9078f, 13.4432f, 55.3781f, -35.5228f };

    // Puzzle
    private bool leftToRight = false;
    private int[] chosenFlags;
    private int[] submitOrder;
    private int submitIndex;
    private int solveFlag;

    // Internals
    private int flagHighlight;
    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private MaskMaterials _maskMaterials;
    private bool _isSolved = false;
    private GameObject[][] checkMarks;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        MaroonButtonSelectable.OnInteract += MaroonButtonPress;
        MaroonButtonSelectable.OnInteractEnded += MaroonButtonRelease;
        _maskMaterials = MaskShaderManager.MakeMaterials();
        Mask.sharedMaterial = _maskMaterials.Mask;

        string[] continentArray = { "Europe", "Asia", "Africa", "South America", "Central America" };
        string chosenDecoyContinent = continentArray.PickRandom();
        continentArray = continentArray.Where(n => n != chosenDecoyContinent).ToArray();
        int[] decoyFlags = Enumerable.Range(0, flagNames.Length).Where(i => continents[i] == chosenDecoyContinent).ToArray();
        string randomContinent = continentArray.PickRandom();
        solveFlag = Enumerable.Range(0, flagNames.Length).Where(i => continents[i] == randomContinent).PickRandom();
        chosenFlags = new[] { solveFlag }.Concat(decoyFlags).ToArray().Shuffle();

        bool isEven = BombInfo.GetSerialNumberNumbers().Last() % 2 == 0;
        leftToRight = Rnd.Range(0, 2) != 0;
        submitOrder = Enumerable.Range(0, chosenFlags.Length).OrderBy(i => isEven ^ (chosenFlags[i] == solveFlag)).ThenBy(i => (leftToRight ? -1 : 1) * flagLatitudes[chosenFlags[i]]).ToArray();

        string decoyStatesString = decoyFlags.Select(i => string.Format("{0} ({1})", flagNames[i], flagLatitudes[i])).Join(", ");
        string submitOrderString = submitOrder.Select(i => flagNames[chosenFlags[i]]).Join(", ");

        Debug.LogFormat(@"[The Maroon Button #{0}] The unique flag is {1} in the continent of {2}, which should be pressed {3}.", _moduleId, flagNames[solveFlag], randomContinent, isEven ? "first" : "last");
        Debug.LogFormat(@"[The Maroon Button #{0}] The decoy flags are {1} in the continent of {2}.", _moduleId, decoyStatesString, chosenDecoyContinent);
        Debug.LogFormat(@"[The Maroon Button #{0}] The correct order is: {1}.", _moduleId, submitOrderString);

        StartCoroutine(AnimateFlagsSequence());
    }

    private void OnDestroy()
    {
        MaskShaderManager.Clear();
    }

    private bool MaroonButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (!_isSolved)
            HandlePress();
        return false;
    }

    private void MaroonButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
    }

    private IEnumerator AnimateFlagsSequence()
    {
        SolveParent.gameObject.SetActive(false);

        var scroller = MakeGameObject("Flags scroller", FlagsParent);
        var width = 0f;
        const float separation = .125f;
        const float spotlightDistance = 1f / 208 * 190;

        checkMarks = Enumerable.Range(0, chosenFlags.Length).Select(_ => new GameObject[2]).ToArray();
        for (var copy = 0; copy < 2; copy++)
        {
            for (int i = 0; i < chosenFlags.Length; i++)
            {
                var flagObj = MakeGameObject(string.Format("Flag {0}", i), scroller.transform,
                    position: new Vector3(width, 0, 0),
                    rotation: Quaternion.Euler(90, 0, 0),
                    scale: new Vector3(.08f, flagSizes[chosenFlags[i]] * 0.001f * 0.08f, 1));
                flagObj.AddComponent<MeshFilter>().sharedMesh = Quad;
                var mr = flagObj.AddComponent<MeshRenderer>();
                mr.material = _maskMaterials.DiffuseTint;
                mr.material.mainTexture = FlagTextures[chosenFlags[i]];

                var checkmarkObj = MakeGameObject(string.Format("Checkmark {0}", i), scroller.transform,
                    position: new Vector3(width, 0, 0),
                    rotation: Quaternion.identity);
                checkmarkObj.AddComponent<MeshFilter>().sharedMesh = Checkmark;
                checkmarkObj.SetActive(false);
                mr = checkmarkObj.AddComponent<MeshRenderer>();
                mr.material = _maskMaterials.DiffuseTint;
                checkMarks[i][copy] = checkmarkObj;

                width += separation;
            }
        }
        width /= 2;

        float scrollFactor = leftToRight ? -.1f : .1f;
        float? solveTime = null;
        const float solveAnimationLength = .63f;

        while (solveTime == null || Time.time < solveTime + solveAnimationLength)
        {
            if (_isSolved && solveTime == null)
                solveTime = Time.time;

            if (solveTime != null)
            {
                // Whoosh away the flags parent
                var st = (Time.time - solveTime.Value) / solveAnimationLength;
                FlagsParent.localPosition = new Vector3(0, 0, Easing.BackIn(st, 0, .2f, 1));
                Spotlight.intensity = 5 * (1 - st);
            }

            float fractionalPosition = ((scrollFactor * Time.time) % width + width) % width + 0.15f;
            scroller.transform.localPosition = new Vector3(-fractionalPosition, -.025f, 0);

            var pos = fractionalPosition / separation;
            var selected = Mathf.RoundToInt(pos);

            // Generated from Maple code; see Blue Button
            var t = pos - selected;
            const float r = -.3f, C1 = -3017.612937f, C2 = 1928.966946f, a = 6198.259105f, q = -.3990297758f, C4 = -525.3291758f, C5 = 461.5871550f;
            var calcAngle =
                t < q ? -.5f * a * Mathf.Pow(t, 2) + C1 * t + C4 :      // = d1(t)
                t < r ? .5f * a * Mathf.Pow(t, 2) + C2 * t + C5 :       // = d2(t)
                180 + Mathf.Atan2(t, spotlightDistance) * 180 / Mathf.PI;   // = d3(t)

            Spotlight.transform.localEulerAngles = new Vector3(40, calcAngle, 0);
            flagHighlight = selected % chosenFlags.Length;
            yield return null;
        }

        FlagsParent.gameObject.SetActive(false);
        Spotlight.intensity = 0;

        // Whoosh in the solve parent
        SolveParent.localPosition = new Vector3(0, 0, -.2f);
        SolveParent.gameObject.SetActive(true);
        SolveFlag.material = _maskMaterials.DiffuseTint;
        SolveFlag.material.mainTexture = FlagTextures[solveFlag];
        SolveFlag.transform.localScale = new Vector3(.1f / (flagSizes[solveFlag] * 0.001f), .1f, 1);
        yield return Animation(solveAnimationLength, t => SolveParent.localPosition = new Vector3(0, 0, Easing.BackOut(t, -.2f, 0, 1)));

        Destroy(scroller);
    }

    private IEnumerator Animation(float duration, Action<float> action)
    {
        var elapsed = 0f;
        while (elapsed < duration)
        {
            action(elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
        action(1);
    }

    private void HandlePress()
    {
        if (_isSolved)
            return;

        if (flagHighlight != submitOrder[submitIndex])
        {
            Debug.LogFormat(@"[The Maroon Button #{0}] Strike! Incorrectly pressed at {1}.", _moduleId, flagNames[chosenFlags[flagHighlight]]);
            Module.HandleStrike();
        }
        else
        {
            StartCoroutine(ShowCheckmark(submitOrder[submitIndex]));

            submitIndex++;

            if (submitIndex == submitOrder.Length)
            {
                Debug.LogFormat(@"[The Maroon Button #{0}] Module solved!", _moduleId);
                _isSolved = true;
                Module.HandlePass();
            }
        }
    }

    private IEnumerator ShowCheckmark(int i)
    {
        MaroonButtonScreen.material = FlagSelectedScreenMaterial;
        yield return Animation(.3f, t =>
        {
            var nt = Easing.BackOut(t, 0, 1, 1);
            foreach (var checkmarkObj in checkMarks[i])
            {
                checkmarkObj.transform.localScale = new Vector3(nt, 1, nt);
                checkmarkObj.GetComponent<MeshRenderer>().material.color = Color.Lerp(CheckmarkHighlightColor, CheckmarkColor, t);
                checkmarkObj.SetActive(t > 0);
            }
        });
        MaroonButtonScreen.material = MaroonButtonScreenMaterial;
    }

    private GameObject MakeGameObject(string name, Transform parent, float scale, Vector3? position = null, Quaternion? rotation = null)
    {
        return MakeGameObject(name, parent, position, rotation, scale: new Vector3(scale, scale, scale));
    }
    private GameObject MakeGameObject(string name, Transform parent, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null)
    {
        var obj = new GameObject(name);
        obj.transform.parent = parent;
        obj.transform.localPosition = position ?? new Vector3(0, 0, 0);
        obj.transform.localRotation = rotation ?? Quaternion.identity;
        obj.transform.localScale = scale ?? new Vector3(1, 1, 1);
        return obj;
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            MaroonButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        MaroonButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*tap\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            MaroonButtonSelectable.OnInteract();
            yield return new WaitForSeconds(.75f);
            MaroonButtonSelectable.OnInteractEnded();
            yield return new WaitForSeconds(.1f);
            yield break;
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        while (!_isSolved)
        {
            yield return true;
            if (flagHighlight == submitOrder[submitIndex])
            {
                MaroonButtonSelectable.OnInteract();
                yield return new WaitForSeconds(.1f);
                MaroonButtonSelectable.OnInteractEnded();
                yield return new WaitForSeconds(.1f);
            }
        }
    }
}
