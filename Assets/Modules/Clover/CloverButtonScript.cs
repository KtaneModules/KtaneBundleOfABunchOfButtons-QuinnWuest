using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class CloverButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable ButtonSelectable;
    public GameObject ButtonCap;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private enum TransColour
    {
        Red,
        Yellow,
        Green,
        Blue,
        Pink,
        White
    }

    private static Dictionary<TransColour, Color> TransColourToColor = new Dictionary<TransColour, Color>()
    {
        { TransColour.Red,      new Color(1, 0.25f, 0.25f) },
        { TransColour.Yellow,   new Color(1, 1, 0.25f) },
        { TransColour.Green,    new Color(0.25f, 1, 0.25f) },
        { TransColour.Blue,     new Color(0.25f, 0.25f, 1) },
        { TransColour.Pink,     new Color(0.25f, 0.625f, 1) },
        { TransColour.White,    Color.white }
    };

    private class TransColourPair
    {
        private TransColour FirstColour;
        private TransColour SecondColour;

        public TransColourPair(TransColour firstColour, TransColour secondColour)
        {
            FirstColour = firstColour;
            SecondColour = secondColour;
        }

        public TransColour GetFirstColour()
        {
            return FirstColour;
        }

        public TransColour GetSecondColour()
        {
            return SecondColour;
        }

        public Color GetFirstColourConverted()
        {
            return TransColourToColor[FirstColour];
        }

        public Color GetSecondColourConverted()
        {
            return TransColourToColor[SecondColour];
        }
    }

    private class Transformation
    {
        private TransColourPair Colours;
        private string Key;
        private Func<string, string> Function;

        public Transformation(TransColourPair colours, string key, Func<string, string> function)
        {
            Colours = colours;
            Key = key;
            Function = function;
        }

        public TransColourPair GetColours()
        {
            return Colours;
        }

        public string GetKey()
        {
            return Key;
        }

        public string Invoke(string input)
        {
            return Function.Invoke(input);
        }
    }

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        ButtonSelectable.OnInteract += ButtonPress;
        ButtonSelectable.OnInteractEnded += ButtonRelease;

        /*string tester = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        Transformation trans = new Transformation(new TransColourPair(TransColour.Red, TransColour.Green), "ABCDEFGHIJKLMNOPQRSTUVWXYZ", delegate (string input) { return input.Reverse().Join(""); });
        Debug.Log(trans.Invoke(tester));*/
    }
    
    private bool ButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        return false;
    }

    private void ButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            ButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        ButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }
}
