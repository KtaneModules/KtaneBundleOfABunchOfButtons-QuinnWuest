using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BunchOfButtonsLib;
using KModkit;
using UnityEngine;
using UnityEngine.Analytics;
using Rnd = UnityEngine.Random;

public class MaroonButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMAudio Audio;
    public KMBombInfo BombInfo;
    public KMSelectable MaroonButtonSelectable;
    public GameObject MaroonButtonCap;

    public MeshRenderer Mask;
    public MaskShaderManager MaskShaderManager;

    // Puzzle
    private static readonly Dictionary<string, int> flagSizes = new Dictionary<string, int> { { "Afghanistan", 667 }, { "Albania", 714 }, { "Algeria", 667 }, { "Andorra", 700 }, { "Angola", 667 }, { "Antigua and Barbuda", 667 }, { "Argentina", 643 }, { "Armenia", 500 }, { "Australia", 500 }, { "Austria", 667 }, { "Azerbaijan", 500 }, { "Bahamas", 500 }, { "Bangladesh", 600 }, { "Barbados", 667 }, { "Belarus", 500 }, { "Belgium", 667 }, { "Belize", 667 }, { "Benin", 667 }, { "Bhutan", 667 }, { "Bolivia", 682 }, { "Bosnia and Herzegovina", 500 }, { "Botswana", 667 }, { "Brazil", 700 }, { "Brunei", 500 }, { "Bulgaria", 600 }, { "Burkina Faso", 667 }, { "Burundi", 600 }, { "Cabo Verde", 588 }, { "Cambodia", 667 }, { "Cameroon", 667 }, { "Canada", 500 }, { "Central African Republic", 667 }, { "Chad", 667 }, { "Chile", 667 }, { "China", 667 }, { "Colombia", 667 }, { "Comoros", 600 }, { "Republic of the Congo", 667 }, { "Costa Rica", 600 }, { "Côte d’Ivoire", 667 }, { "Croatia", 500 }, { "Cuba", 500 }, { "Cyprus", 600 }, { "Czech Republic", 667 }, { "North Korea", 500 }, { "Democratic Republic of the Congo", 667 }, { "Denmark", 757 }, { "Djibouti", 667 }, { "Dominica", 500 }, { "Dominican Republic", 625 }, { "Ecuador", 500 }, { "Egypt", 667 }, { "El Salvador", 564 }, { "Equatorial Guinea", 667 }, { "Eritrea", 500 }, { "Estonia", 636 }, { "Eswatini", 667 }, { "Ethiopia", 500 }, { "Fiji", 500 }, { "Finland", 611 }, { "France", 667 }, { "Gabon", 750 }, { "The Gambia", 667 }, { "Georgia", 667 }, { "Germany", 600 }, { "Ghana", 667 }, { "Greece", 667 }, { "Grenada", 600 }, { "Guatemala", 625 }, { "Guinea", 667 }, { "Guinea-Bissau", 500 }, { "Guyana", 600 }, { "Haiti", 600 }, { "Honduras", 500 }, { "Hungary", 500 }, { "Iceland", 720 }, { "India", 667 }, { "Iran", 571 }, { "Iraq", 667 }, { "Ireland", 500 }, { "Israel", 727 }, { "Italy", 667 }, { "Jamaica", 500 }, { "Japan", 667 }, { "Jordan", 500 }, { "Kazakhstan", 500 }, { "Kenya", 667 }, { "Kiribati", 500 }, { "Kuwait", 500 }, { "Kyrgyzstan", 600 }, { "Laos", 667 }, { "Latvia", 500 }, { "Lebanon", 667 }, { "Lesotho", 667 }, { "Liberia", 526 }, { "Libya", 500 }, { "Liechtenstein", 600 }, { "Lithuania", 600 }, { "Luxembourg", 600 }, { "Madagascar", 667 }, { "Malawi", 667 }, { "Malaysia", 500 }, { "Maldives", 667 }, { "Mali", 667 }, { "Malta", 667 }, { "Marshall Islands", 526 }, { "Mauritania", 667 }, { "Mauritius", 667 }, { "Mexico", 571 }, { "Federated States of Micronesia", 526 }, { "Mongolia", 500 }, { "Montenegro", 500 }, { "Morocco", 667 }, { "Mozambique", 667 }, { "Myanmar", 556 }, { "Namibia", 667 }, { "Nauru", 500 }, { "Nepal", 1222 }, { "Netherlands", 667 }, { "New Zealand", 500 }, { "Nicaragua", 600 }, { "Niger", 857 }, { "Nigeria", 500 }, { "North Macedonia", 500 }, { "Norway", 727 }, { "Oman", 500 }, { "Pakistan", 667 }, { "Palau", 625 }, { "Panama", 667 }, { "Papua New Guinea", 750 }, { "Paraguay", 600 }, { "Peru", 667 }, { "Philippines", 500 }, { "Poland", 625 }, { "Portugal", 667 }, { "South Korea", 667 }, { "Moldova", 500 }, { "Romania", 667 }, { "Russia", 667 }, { "Rwanda", 667 }, { "Saint Kitts and Nevis", 667 }, { "Saint Lucia", 500 }, { "Saint Vincent and the Grenadines", 667 }, { "Samoa", 500 }, { "San Marino", 750 }, { "São Tomé and Príncipe", 500 }, { "Saudi Arabia", 667 }, { "Senegal", 667 }, { "Serbia", 667 }, { "Seychelles", 500 }, { "Sierra Leone", 667 }, { "Singapore", 667 }, { "Slovakia", 667 }, { "Slovenia", 500 }, { "Solomon Islands", 500 }, { "Somalia", 667 }, { "South Africa", 667 }, { "South Sudan", 500 }, { "Spain", 667 }, { "Sri Lanka", 500 }, { "Sudan", 500 }, { "Suriname", 667 }, { "Sweden", 625 }, { "Switzerland", 1000 }, { "Syria", 667 }, { "Tajikistan", 500 }, { "Thailand", 667 }, { "Timor-Leste", 500 }, { "Togo", 618 }, { "Tonga", 500 }, { "Trinidad and Tobago", 600 }, { "Tunisia", 667 }, { "Turkey", 667 }, { "Turkmenistan", 667 }, { "Tuvalu", 500 }, { "Uganda", 667 }, { "Ukraine", 667 }, { "United Arab Emirates", 500 }, { "United Kingdom", 500 }, { "Tanzania", 667 }, { "United States", 526 }, { "Uruguay", 667 }, { "Uzbekistan", 500 }, { "Vanuatu", 600 }, { "Venezuela", 667 }, { "Vietnam", 667 }, { "Yemen", 667 }, { "Zambia", 667 }, { "Zimbabwe", 500 } };
    public Transform FlagsParent;
    private bool leftToRight = false;
    private int[] chosenFlags;
    private int[] submitOrder;
    private int submitIndex;
    public Texture[] FlagTextures;
    public static readonly string[] continents = { "Africa", "Africa", "South America", "Asia", "South America", "Europe", "South America", "Europe", "Europe", "Central America", "Central America", "Asia", "Asia", "Central America", "Central America", "Africa", "Asia", "Africa", "Europe", "South America" };
    public static readonly float[] flagLatitudes = { 28.0339f, 11.2027f, -14.2350f, 35.8617f, 4.5709f, 49.8175f, -1.8312f, 46.2276f, 51.1657f, 15.7835f, 15.2f, 20.5937f, 36.2048f, 12.8654f, 8.538f, 14.4974f, 35.9078f, 13.4432f, 55.3781f, -35.5228f };
    public Mesh Quad;
    public Light Spotlight;
    private int flagHighlight;

    // Internals
    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private MaskMaterials _maskMaterials;
    private bool _isSolved = false;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        MaroonButtonSelectable.OnInteract += MaroonButtonPress;
        MaroonButtonSelectable.OnInteractEnded += MaroonButtonRelease;
        _maskMaterials = MaskShaderManager.MakeMaterials();
        Mask.sharedMaterial = _maskMaterials.Mask;

        GeneratePuzzle();

        StartCoroutine(AnimateFlagsSequence());
    }

    private void GeneratePuzzle()
    {
        string[] continentArray = { "Europe", "Asia", "Africa", "South America", "Central America" };
        string chosenDecoyContinent = continentArray.PickRandom();
        continentArray = continentArray.Where(n => n != chosenDecoyContinent).ToArray();
        int[] decoyFlags = Enumerable.Range(0, FlagTextures.Length).Where(i => continents[i] == chosenDecoyContinent).ToArray();
        string randomContinent = continentArray.PickRandom();
        int chosenFlag = Enumerable.Range(0, FlagTextures.Length).Where(i => continents[i] == randomContinent).PickRandom();
        chosenFlags = new[] { chosenFlag }.Concat(decoyFlags).ToArray().Shuffle();

        bool isEven = BombInfo.GetSerialNumberNumbers().Last() % 2 == 0;
        leftToRight = Rnd.Range(0, 2) != 0;
        submitOrder = Enumerable.Range(0, chosenFlags.Length).OrderBy(i => isEven ^ (chosenFlags[i] == chosenFlag)).ThenBy(i => (leftToRight ? -1 : 1) * flagLatitudes[chosenFlags[i]]).ToArray();

        string decoyStatesString = decoyFlags.Select(i => string.Format("{0} ({1})", FlagTextures[i].name, flagLatitudes[i])).Join(", ");
        string submitOrderString = submitOrder.Select(i => FlagTextures[chosenFlags[i]].name).Join(", ");

        Debug.LogFormat(@"[The Maroon Button #{0}] The unique flag is {1} in the continent of {2}.", _moduleId, FlagTextures[chosenFlag].name, randomContinent, flagLatitudes[chosenFlag]);
        Debug.LogFormat(@"[The Maroon Button #{0}] The decoy flags are {1} in the continent of {2}.", _moduleId, decoyStatesString, chosenDecoyContinent);
        Debug.LogFormat(@"[The Maroon Button #{0}] The correct order is: {1}.", _moduleId, submitOrderString);
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
        var scroller = MakeGameObject("Flags scroller", FlagsParent);
        var width = 0f;
        var numCopies = 0;
        const float separation = .125f;
        const float spotlightDistance = 1f / 208 * 190;

        while (numCopies < 2)
        {
            for (int i = 0; i < chosenFlags.Length; i++)
            {
                var blobObj = MakeGameObject(string.Format("Flag {0}", i), scroller.transform, position: new Vector3(width, 0, 0), rotation: Quaternion.Euler(90, 0, 0), scale: new Vector3(.08f, flagSizes[FlagTextures[chosenFlags[i]].name] * 0.001f * 0.08f, 1));
                blobObj.AddComponent<MeshFilter>().sharedMesh = Quad;
                var mr = blobObj.AddComponent<MeshRenderer>();
                mr.material = _maskMaterials.DiffuseTint;
                mr.material.mainTexture = FlagTextures[chosenFlags[i]];
                width += separation;
            }
            numCopies++;
        }
        width /= numCopies;

        float scrollFactor = leftToRight ? -.1f : .1f;
        while (!_isSolved)
        {
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

        Destroy(scroller);
    }

    private void HandlePress()
    {
        if (_isSolved)
            return;

        if (flagHighlight != submitOrder[submitIndex])
        {
            Debug.LogFormat(@"[The Maroon Button #{0}] Strike! Incorrectly pressed at {1}.", _moduleId, FlagTextures[chosenFlags[flagHighlight]].name);
            Module.HandleStrike();
        }
        else
        {
            submitIndex++;
            if (submitIndex == submitOrder.Length)
            {
                Debug.LogFormat(@"[The Maroon Button #{0}] Module solved!", _moduleId);
                _isSolved = true;
                Module.HandlePass();
            }
        }
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
        yield return null;
    }
}
