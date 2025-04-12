using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class CornflowerButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable ButtonSelectable;
    public KMBossModule BossModule;

    public GameObject ButtonCap;
    public Transform MainArrowRotator;
    public Transform[] ArrowRotators;
    public Material NodeMat;
    public Material NodeLitMat;
    public MeshRenderer[] Nodes;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;
    private bool _inUnity;
    private Type _selectableType;
    private Type _alarmClockType;
    private FieldInfo _childrenField;
    private FieldInfo _onHighlightField;
    private FieldInfo _onHighlightEndedField;
    private FieldInfo _onDefocusField;
    private Coroutine _highlightEndedCoroutine;

    private List<ParentChildPair> _selectables = new List<ParentChildPair>();
    private int _curSelectable;

    private readonly int[] _arrowPositions = { 0, 0, 0 };
    private readonly Quaternion[] _arrowAngles = new Quaternion[3];
    private float _mainArrowAngle;
    private int _selectedArrows;
    private Component _lastHighlighted;
    private bool _initialized;
    private string[] _ignoreList;
    private static readonly string[] _defaultIgnoreList = { "Cursor Maze", "Hidden In Plain Sight" };

    private readonly HashSet<Component> _assigned = new HashSet<Component>();

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        ButtonSelectable.OnInteract += ButtonPress;
        ButtonSelectable.OnInteractEnded += ButtonRelease;


        do
        {
            _arrowPositions[0] = Rnd.Range(0, 5);
            _arrowPositions[1] = Rnd.Range(0, 5);
            _arrowPositions[2] = Rnd.Range(0, 5);
        }
        while (_arrowPositions.All(p => p == 0));

        _ignoreList = BossModule.GetIgnoredModules(Module, _defaultIgnoreList);

        if (Application.isEditor)
        {
            Debug.LogFormat("[The Cornflower Button #{0}] In Unity scene. Press the module to solve.", _moduleId);
            _inUnity = true;
            return;
        }
        StartCoroutine(Init());
    }

    struct ParentChildPair
    {
        public Component Parent;    // actually the ‘Selectable’ component
        public Component Child;
    }

    private IEnumerator Init()
    {
        yield return null;

        Debug.LogFormat("<The Cornflower Button #{0}> Finding Selectable type", _moduleId);
        var asm = GetComponents<Component>().First(c => HasBaseType(c.GetType(), "Selectable")).GetType().Assembly;

        _selectableType = asm.GetType("Selectable");
        _alarmClockType = asm.GetType("Assets.Scripts.Props.AlarmClock");
        _childrenField = _selectableType.GetField("Children", BindingFlags.Instance | BindingFlags.Public);

        _onHighlightField = _selectableType.GetField("OnHighlight", BindingFlags.Instance | BindingFlags.Public);
        _onHighlightEndedField = _selectableType.GetField("OnHighlightEnded", BindingFlags.Instance | BindingFlags.Public);
        _onDefocusField = _selectableType.GetField("OnDefocus", BindingFlags.Instance | BindingFlags.Public);

        var gameObjs = Enumerable.Range(0, transform.parent.childCount).Select(ix => transform.parent.GetChild(ix)).ToArray();
        var alarmClock = ((MonoBehaviour) FindObjectOfType(_alarmClockType)).gameObject.GetComponent(_selectableType);
        var selectableParents = new List<Component> { alarmClock };

        // Find out which face we’re on
        Debug.LogFormat("<The Cornflower Button #{0}> Finding faces", _moduleId);
        var faces = transform.parent.gameObject.GetComponentsInChildren<KMBombFace>(true);
        if (faces.Length >= 2)
        {
            var frontFaceRot = faces[0].transform.localRotation;
            var rearFaceRot = faces[1].transform.localRotation;
            var onFrontFace = Quaternion.Angle(frontFaceRot, transform.localRotation) < Quaternion.Angle(rearFaceRot, transform.localRotation);

            // Find all the other modules on the same face
            selectableParents.AddRange(gameObjs.Select(t =>
            {
                if (t.gameObject == gameObject || (Quaternion.Angle(frontFaceRot, t.localRotation) < Quaternion.Angle(rearFaceRot, t.localRotation)) != onFrontFace)
                    return null;
                var comps = t.GetComponents<Component>();
                if (!comps.Any(c => c != null && HasBaseType(c.GetType(), "BombComponent")))
                    return null;
                var m = comps.OfType<KMBombModule>().FirstOrDefault();
                if (m != null && _ignoreList.Contains(m.ModuleDisplayName))
                    return null;
                return comps.FirstOrDefault(c => c != null && HasBaseType(c.GetType(), "Selectable"));
            }).Where(c => c != null));
        }

        foreach (var parent in selectableParents)
        {
            AssignDelegate(parent, _onDefocusField, HandleDefocus(parent));
            RediscoverSelectables(parent, false);
            StartCoroutine(PeriodicSelectableDiscovery(parent));
        }
        setSelectedArrows(0);
        setCurSelectable(Rnd.Range(0, _selectables.Count));
        _initialized = true;
    }

    private IEnumerator PeriodicSelectableDiscovery(Component parent)
    {
        yield return new WaitForSeconds(Rnd.Range(0f, 10f));
        while (true)
        {
            RediscoverSelectables(parent, true);
            yield return new WaitForSeconds(10f);
        }
    }

    private Action HandleDefocus(Component parent)
    {
        return delegate
        {
            if (ReferenceEquals(parent, _selectables[_curSelectable].Parent) && ReferenceEquals(_lastHighlighted, _selectables[_curSelectable].Child))
            {
                do
                    setCurSelectable((_curSelectable + 1) % _selectables.Count);
                while (!_selectables[_curSelectable].Child.gameObject.activeInHierarchy);
                setSelectedArrows(_selectedArrows + 1);
            }
        };
    }

    private void setSelectedArrows(int selArrows)
    {
        _selectedArrows = selArrows % 3;
        for (var i = 0; i < Nodes.Length; i++)
            Nodes[i].sharedMaterial = NodeMat;
        for (var i = 0; i < 2; i++)
            Nodes[(_selectedArrows + i) % Nodes.Length].sharedMaterial = NodeLitMat;
    }

    private Vector3 childPos(Component child)
    {
        var renderer = child.GetComponent<Renderer>() ?? child.GetComponentInChildren<Renderer>();
        return renderer != null ? renderer.bounds.center : child.transform.position;
    }

    private float childDist(Component child)
    {
        var otherPoint = transform.InverseTransformPoint(childPos(child));
        otherPoint.y = 0;
        return otherPoint.magnitude;
    }

    private float childAngle(Component child)
    {
        var otherPoint = transform.InverseTransformPoint(childPos(child));
        otherPoint.y = 0;
        return Vector3.SignedAngle(Vector3.forward, otherPoint, Vector3.up);
    }

    private Component[] getChildren(Component parent)
    {
        var arr = _childrenField.GetValue(parent) as Array;
        return arr == null ? null : arr.Cast<Component>().ToArray();
    }

    private void RediscoverSelectables(Component parent, bool findNewSelectable)
    {
        var has = _selectables.Count > 0;
        var oldPair = has ? _selectables[_curSelectable] : default(ParentChildPair);
        var oldDist = has ? childDist(_selectables[_curSelectable].Child) : 0;

        _selectables.RemoveAll(pair => ReferenceEquals(pair.Parent, parent));

        if (getChildren(parent) != null)
            foreach (var child in getChildren(parent).Where(c => c != null).Distinct())
            {
                if (_assigned.Add(child))
                {
                    AssignDelegate(child, _onHighlightField, delegate
                    {
                        if (_highlightEndedCoroutine != null)
                            StopCoroutine(_highlightEndedCoroutine);
                        _lastHighlighted = child;
                    });

                    AssignDelegate(child, _onHighlightEndedField, delegate
                    {
                        if (_highlightEndedCoroutine != null)
                            StopCoroutine(_highlightEndedCoroutine);
                        _highlightEndedCoroutine = StartCoroutine(endHighlight(child));
                    });
                }
                _selectables.Add(new ParentChildPair { Parent = parent, Child = child });
            }
        SortSelectables();

        int newSelectable = _curSelectable;
        if (findNewSelectable)
        {
            newSelectable = 0;
            if (has)
            {
                int p;
                if ((p = _selectables.IndexOf(pair => ReferenceEquals(pair.Parent, oldPair.Parent) && ReferenceEquals(pair.Child, oldPair.Child) && pair.Child.gameObject.activeInHierarchy)) != -1)
                    newSelectable = p;
                else if ((p = _selectables.IndexOf(pair => childDist(pair.Child) >= oldDist && pair.Child.gameObject.activeInHierarchy)) != -1)
                    newSelectable = p;
            }
            setCurSelectable(newSelectable);
        }
    }

    private void SortSelectables()
    {
        _selectables.Sort((a, b) => Math.Sign(childDist(a.Child) - childDist(b.Child)));

        var last = _selectables[0];
        var newList = new List<ParentChildPair> { last };
        _selectables.RemoveAt(0);
        while (_selectables.Count > 0)
        {
            var p = _selectables.IndexOf(pair => !ReferenceEquals(pair.Parent, last.Parent));
            if (p == -1)
                p = 0;
            last = _selectables[p];
            _selectables.RemoveAt(p);
            newList.Add(last);
        }
        _selectables = newList;
    }

    private ParentChildPair _prevSelectablePair;
    private void setCurSelectable(int newSelectable)
    {
        if (_selectables.Count == 0)
            return;
        var newSelectablePair = _selectables[newSelectable];
        if (!ReferenceEquals(newSelectablePair.Parent, _prevSelectablePair.Parent) || !ReferenceEquals(newSelectablePair.Child, _prevSelectablePair.Child))
            Debug.LogFormat("[The Cornflower Button #{0}] Target selectable: {1}", _moduleId, GetObjectPath(newSelectablePair.Child.transform));
        _prevSelectablePair = newSelectablePair;
        _curSelectable = newSelectable;
    }

    private string GetObjectPath(Transform tr)
    {
        var s = new List<string>();
        while (tr != null)
        {
            s.Add(tr.name);
            tr = tr.parent;
        }
        s.Reverse();
        return s.Join(" → ");
    }

    private IEnumerator endHighlight(Component child)
    {
        yield return null;
        if (ReferenceEquals(_lastHighlighted, child))
            _lastHighlighted = null;
    }

    private void AssignDelegate(object obj, FieldInfo field, Action action)
    {
        Action deleg = (Action) field.GetValue(obj);
        deleg += action;
        field.SetValue(obj, deleg);
    }

    private bool ButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (_moduleSolved)
            return false;

        if (_inUnity)
        {
            _moduleSolved = true;
            Module.HandlePass();
            return false;
        }

        for (var i = 0; i < 2; i++)
        {
            var pos = (_selectedArrows + i) % 3;
            _arrowPositions[pos] = (_arrowPositions[pos] + 1) % 5;
        }

        if (_arrowPositions.All(p => p == 0))
        {
            Debug.LogFormat("[The Cornflower Button #{0}] Module solved.", _moduleId);
            _moduleSolved = true;
            Module.HandlePass();
        }
        return false;
    }

    void Update()
    {
        if (!_initialized)
            return;

        for (var i = 0; i < 3; i++)
        {
            _arrowAngles[i] = Quaternion.Slerp(_arrowAngles[i], Quaternion.Euler(0, 180 + _arrowPositions[i] * 72, 0), 4 * Time.deltaTime);
            ArrowRotators[i].localRotation = _arrowAngles[i];
        }

        tryAgain:
        var curChildren = (Array) _childrenField.GetValue(_selectables[_curSelectable].Parent);
        if (!_selectables[_curSelectable].Child.gameObject.activeInHierarchy
                || curChildren == null
                || !Enumerable.Range(0, curChildren.Length).Any(ix => ReferenceEquals(curChildren.GetValue(ix), _selectables[_curSelectable].Child)))
        {
            RediscoverSelectables(_selectables[_curSelectable].Parent, true);
            goto tryAgain;
        }

        var desiredAngleMain = childAngle(_selectables[_curSelectable].Child);
        _mainArrowAngle = Mathf.LerpAngle(_mainArrowAngle, desiredAngleMain, 4 * Time.deltaTime);
        MainArrowRotator.localEulerAngles = new Vector3(0, _mainArrowAngle, 0);
    }

    private bool HasBaseType(Type type, string name)
    {
        return type != null && (type.Name == name || HasBaseType(type.BaseType, name));
    }

    // Unused function used only for debugging
    private List<string> Dump(GameObject obj)
    {
        var deepTypes = new string[] { };

        var strs = new List<string> { string.Format("- {0}:", obj.name) };
        foreach (var component in obj.GetComponents<Component>())
        {
            strs.Add(string.Format("      • {0}", component.GetType().FullName));
            if (deepTypes.Contains(component.GetType().FullName))
            {
                foreach (var field in component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var val = field.GetValue(component);
                    strs.Add(string.Format("          - {4}.{0} ({1}) = {2} ({3})", field.Name, field.FieldType, val ?? "<null>", val == null ? "<null>" : val.GetType().FullName, field.DeclaringType.FullName));
                }
            }
        }
        for (var i = 0; i < obj.transform.childCount; i++)
        {
            var child = Dump(obj.transform.GetChild(i).gameObject);
            foreach (var str in child)
                strs.Add("      " + str);
        }
        return strs;
    }

    private IEnumerable<FieldInfo> GetAllFields(Type type, bool privateOnly = false)
    {
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | (privateOnly ? 0 : BindingFlags.Public)))
            yield return field;
        if (type.BaseType != null)
            foreach (var field in GetAllFields(type.BaseType, true))
                yield return field;
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
