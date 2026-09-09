using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIPanelSystem : MonoBehaviour
{
    EventSystem eventSystem;

    [System.Serializable]
    public class Panel
    {
        [Header("Main")]
        [HideInInspector] public Vector3 originalAnchorPosition;
        public string panelName;
        public bool open;
        public GameObject panelObject;

        [Header("Non-mouse selection")]
        public Selectable startingSelection;

        [Header("Button / Interactables")]
        public List<GameObject> interactables;
        public List<GameObject> openButtons;
        public List<GameObject> closeButtons;

        [Header("Background panel")]
        public List<string> coPanels;
        public List<string> inactiveCoPanels;

        [Header("Animation management")]
        public bool animated;
        public float timeToEnd;
    }

    [System.Serializable]
    public class Buttons
    {
        [Header("Main")]
        public string buttonName;
        public GameObject gameObject;

        [Header("Special Effect")]
        public string transitionToScene;
        public bool quitProgram;
        public bool exitPause;
    }

    [System.Serializable]
    public class Sliders
    {
        [Header("Main")]
        public string buttonName;
        public GameObject gameObject;
    }
    [Header("States")]
    public bool currentlyPaused;
    [Header("Modifiers")]
    public bool locationChangeOnClose;
    [Header("Lists")]
    public List<Panel> panels;
    public List<Buttons> buttons;
    public List<Sliders> sliders;

    // helper function to activate or deactivate panel go or just move them out of the way
    public void ActivePanel(Panel panel, bool open)
    {
        if (locationChangeOnClose)
        {
            panel.panelObject.SetActive(true);
            panel.panelObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, open ? 0 : 9999, 0);
        }
        else
        {
            panel.panelObject.SetActive(open);
        }
    }

    void Start()
    {
        Time.timeScale = 1;
        eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();

        //Cursor.visible = false;
        //Cursor.lockState = CursorLockMode.Locked;

        StartSlideChecker();

        // ensure everything is closed at start
        foreach (var p in panels)
        {
            p.originalAnchorPosition = p.panelObject.GetComponent<RectTransform>().anchoredPosition;
            if (p.panelObject != null)
            {
                ActivePanel(p, false);
                //p.panelObject.SetActive(false);
            }

            SetInteractableList(p.interactables, false);
        }

        OpenPanel(panels[0].panelName);
    }

    // simple implimentations for scene transistions and or panel manipulation
    public void ButtonPressed(GameObject button)
    {
        string btnName = button.name;

        foreach (Buttons but in buttons)
        {
            if (but.buttonName == btnName)
            {
                if (but.exitPause)
                {
                    Pause();
                    return;
                }
                else if (but.quitProgram)
                {
                    Application.Quit();
                    return;
                }
                else
                {
                    print(but.buttonName);
                    print(but.transitionToScene);
                    SceneManager.LoadScene(but.transitionToScene);
                    return;
                }
            }
        }
        // Check if button opens a panel
        foreach (var panel in panels)
        {
            if (panel.openButtons != null && panel.openButtons.Contains(button))
            {
                OpenPanel(panel.panelName);
                return;
            }
        }

        // Check close buttons for each panel
        foreach (var panel in panels)
        {
            if (panel.closeButtons != null && panel.closeButtons.Contains(button))
            {
                ClosePanel(panel.panelName);
                return;
            }
        }
    }

    // Panel Opening Logic
    public void OpenPanel(string selectedName)
    {
        Panel panel = GetPanel(selectedName);
        if (panel == null) return;

        // enable this panel
        if (panel.panelObject != null)
        {
            if (panel.animated && !panel.open)
            {
                ActivePanel(panel, true);
                //panel.panelObject.SetActive(true);
                Animator anim = panel.panelObject.GetComponent<Animator>();
                anim.SetTrigger("Open");
                //eventSystem.SetSelectedGameObject(panel.startingSelection.gameObject);
                //StartCoroutine(AnimationStart(panel.timeToEnd, panel));
            }
            else
            {
                ActivePanel(panel, true);
                //panel.panelObject.SetActive(true);

                //eventsystem selector
                //eventSystem.SetSelectedGameObject(panel.startingSelection.gameObject);
            }
            panel.open = true;
        }

        // make interactables active
        SetInteractableList(panel.interactables, true);

        // Close all other panels unless listed as a coPanel
        foreach (var other in panels)
        {
            if (other.panelName == selectedName) continue;

            bool stayOpen = panel.coPanels != null && panel.coPanels.Contains(other.panelName);
            bool stayOpenDisabled = panel.inactiveCoPanels != null && panel.inactiveCoPanels.Contains(other.panelName);

            if (!stayOpen && !stayOpenDisabled)
            {
                if (other.panelObject != null)
                {
                    if (other.animated && other.open == true)
                    {
                        Animator anim = other.panelObject.GetComponent<Animator>();
                        anim.SetTrigger("Close");
                        StartCoroutine(AnimationEnd(other.timeToEnd, other.panelObject));
                    }
                    else
                    {
                        ActivePanel(other, false);
                        //other.panelObject.SetActive(false);
                    }
                    other.open = false;
                }

                SetInteractableList(other.interactables, false);
            }
            else if (stayOpenDisabled)
            {
                // make sure it's visible
                if (other.panelObject != null && !other.open)
                {
                    ActivePanel(other, true);
                    //other.panelObject.SetActive(true);
                }

                // disable interaction
                SetInteractableList(other.interactables, false);
                other.open = true;
            }
            else if (stayOpen)
            {
                // make sure it's visible
                if (other.panelObject != null && !other.open)
                {
                    ActivePanel(other, true);
                    //other.panelObject.SetActive(true);
                }

                // enable interaction
                SetInteractableList(other.interactables, true);
                other.open = true;
            }
        }
    }

    public void ClosePanel(string name)
    {
        Panel panel = GetPanel(name);
        if (panel == null) return;

        // disable its object and interaction
        if (panel.panelObject != null)
        {
            if (panel.animated && panel.open)
            {
                Animator anim = panel.panelObject.GetComponent<Animator>();
                anim.SetTrigger("Close");
                StartCoroutine(AnimationEnd(panel.timeToEnd, panel.panelObject));
            }
            else
            {
                ActivePanel(panel, false);
                //panel.panelObject.SetActive(false);
            }
            panel.open = false;
        }

        SetInteractableList(panel.interactables, false);

        // Check if *any* other panel is still open
        foreach (var p in panels)
        {
            if (p.panelObject != null && p.panelObject.activeSelf)
            {
                break;
            }
        }
    }

    // ------------------------------------------------------------
    // Utility
    // ------------------------------------------------------------

    public IEnumerator AnimationEnd(float timeToEnd, GameObject panel)
    {
        yield return new WaitForSeconds(timeToEnd);
        //ActivePanel(panel, false); // NEED TO FIX LATER FOR ANIMATION IMPLEMENTATION
        //panel.SetActive(false);
    }
    public IEnumerator AnimationStart(float timeToEnd, Panel panel)
    {
        yield return new WaitForSeconds(timeToEnd);
        //eventSystem.SetSelectedGameObject(panel.startingSelection.gameObject);
    }

    Panel GetPanel(string name)
    {
        foreach (var p in panels)
            if (p.panelName == name)
                return p;

        return null;
    }

    void SetInteractableList(List<GameObject> list, bool state)
    {
        if (list == null) return;

        foreach (var obj in list)
        {
            if (obj == null) continue;

            var btn = obj.GetComponent<Button>();
            if (btn != null) { btn.interactable = state; continue; }

            var slider = obj.GetComponent<Slider>();
            if (slider != null) { slider.interactable = state; continue; }

            var toggle = obj.GetComponent<Toggle>();
            if (toggle != null) { toggle.interactable = state; continue; }
        }
    }

    void Update()
    {
        SlideChecker(); // you could refactor for onvaluechange but IDK how that unity event works
    }

    void StartSlideChecker()
    {
        if (sliders.Count > 0)
        {
            foreach (Sliders s in sliders)
            {
                if (s != null)
                {
                    // if (s.name == "MUSIC")
                    // {
                    //     var e = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
                    //     //print(e);
                    //     s.value = e;
                    // }
                    // else if (s.name == "SFX")
                    // {
                    //     var e = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
                    //     //print(e);
                    //     s.value = e;
                    // }
                }
            }
        }
    }
    void SlideChecker()
    {
        if (sliders.Count > 0)
        {
            foreach (Sliders s in sliders)
            {
                if (s != null)
                {
                    // if (s.name == "MUSIC")
                    // {
                    //     float value = s.value;
                    //     PlayerPrefs.SetFloat("MusicVolume", value);
                    //     AudioManager.instance.SetBGMParameter("Volume", value);
                    //     PlayerPrefs.Save();
                    // }
                    // else if (s.name == "SFX")
                    // {
                    //     float value = s.value;
                    //     PlayerPrefs.SetFloat("SFXVolume", value);
                    //     AudioManager.instance.SetSFXParameter(value);
                    //     PlayerPrefs.Save();

                    // }
                }
            }
        }
    }

    public void OnPause(InputValue value)
    {
        print("heehee");
    }

    public void Pause()
    {
        if (currentlyPaused == true)
        {
            ClosePanel("PauseMenu");
            currentlyPaused = false;
            Time.timeScale = 1;
        }
        else if (currentlyPaused == false)
        {
            OpenPanel("PauseMenu");
            currentlyPaused = true;
            Time.timeScale = 0;
        }
    }
}