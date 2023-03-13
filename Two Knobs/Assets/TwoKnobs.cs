using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class TwoKnobs : MonoBehaviour
{

    public KMBombInfo Bomb;
    public KMAudio Audio;

    public KMSelectable knobG, knobW;
    public TextMesh displayNumber;
    public Color[] colors;

    public float delay = 2.5f;
    float timer;
    int interval, initialDisplay, displayedNum, digitalKey, cdKey, pressCount, clickDelay;

    bool inputPhase = false;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;


        /*
        foreach (KMSelectable object in keypad) {
            object.OnInteract += delegate () { keypadPress(object); return false; };
        }
        */

        knobG.OnInteract += delegate () { GreyButtonPress(); return false; };

        knobW.OnInteract += delegate () { WhiteButtonPress(); return false; };

    }
    void GreyButtonPress()
    {
        if (ModuleSolved)
            return;
        if (!inputPhase)
        {
            StartInputPhase();
        }
        else
            return;
    }
    void WhiteButtonPress()
    {
        if (!inputPhase || ModuleSolved)
            return;
        else if (clickDelay > 0)
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, knobW.transform);
        else if (Bomb.GetFormattedTime().EndsWith(cdKey.ToString()) || displayedNum == digitalKey)
        {
            knobW.transform.Rotate(0, 45, 0);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, knobW.transform);
            pressCount++;
            clickDelay = 50;
            if (pressCount >= 8)
            {
                GetComponent<KMBombModule>().HandlePass();
                ModuleSolved = true;
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, knobW.transform);
                ResetInputPhase();
                displayNumber.text = ":)";
            }
            else
                Debug.LogFormat("[Two Knobs #{0}] Knob correctly turned. Presses left: {1}", ModuleId, 8 - pressCount);
        }
        else
        {
            GetComponent<KMBombModule>().HandleStrike();
            Debug.LogFormat("[Two Knobs #{0}] Strike! Display number was {1}, and time pressed was {2}.", ModuleId, displayNumber, Bomb.GetFormattedTime());
            ResetInputPhase();
        }
    }
    void StartInputPhase()
    {
        inputPhase = true;
        displayNumber.color = colors[1]; //light red
        displayedNum = 1;
        displayNumber.text = displayedNum + "";
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, knobG.transform);
    }
    void ResetInputPhase()
    {
        inputPhase = false;
        pressCount = 0;
        displayNumber.color = colors[0]; //white
        displayedNum = initialDisplay;
        displayNumber.text = displayedNum + "";
    }
    void Start()
    {
        pressCount = 0;
        initialDisplay = Rnd.Range(0, 11);
        displayedNum = initialDisplay;
        displayNumber.text = displayedNum + "";
        displayNumber.color = colors[0]; //white

        int[] serialNums = Bomb.GetSerialNumberNumbers().ToArray();

        interval = Rnd.Range(1, 6);
        digitalKey = DigitalRoot(serialNums) + interval;
        digitalKey %= 5;
        if (digitalKey == 0)
            digitalKey += 5;

        calculateCDKey();

        Debug.LogFormat("[Two Knobs #{0}] The increase interval is {1}. The digital key is {2}. The countdown key is {3}.", ModuleId, interval, digitalKey, cdKey);

        StartCoroutine(SlowRotate());
    }

    void Update()
    {
        //if (solvedModules < Bomb.GetSolvedModuleNames().Count())
        //{
        //    solvedModules++;
        //    calculateCDKey();           
        //}
        if (ModuleSolved)
            return;
        if (!inputPhase)
        {
            timer += Time.deltaTime;
            if (timer > delay)
            {
                ChangeNumber();
                timer = 0;
            }
        }
        else
        {
            timer += Time.deltaTime;
            if (timer > delay)
            {
                ChangeNumberInput();
                timer = 0;
            }
        }
        if (clickDelay > 0)
            clickDelay--;

    }
    void calculateCDKey()
    {
        int trueRows = 0;
        int total = 0;

        int[] battTypeCount = calculateBatteryTypeCount();
        int dBattCount = battTypeCount[0];
        int aaBattCount = battTypeCount[1];

        if (Bomb.GetBatteryCount() > 2)
        {
            trueRows++;
            Debug.LogFormat("[Two Knobs #{0}] Row {1} is applicable.", ModuleId, 0);
        }
        if (Bomb.IsIndicatorOn(Indicator.FRK))
        {
            trueRows++;
            total++;
            Debug.LogFormat("[Two Knobs #{0}] Row {1} is applicable.", ModuleId, 1);
        }
        if (Bomb.IsPortPresent(Port.Serial))
        {
            trueRows++;
            total += 2;
            Debug.LogFormat("[Two Knobs #{0}] Row {1} is applicable.", ModuleId, 2);
        }
        if (Bomb.GetSerialNumberNumbers().ElementAt(Bomb.GetSerialNumberNumbers().Count() - 1) % 2 == 1)
        {
            trueRows++;
            total += 3;
            Debug.LogFormat("[Two Knobs #{0}] Row {1} is applicable.", ModuleId, 3);
        }
        if (dBattCount > aaBattCount)
        {
            trueRows++;
            total += 4;
            Debug.LogFormat("[Two Knobs #{0}] Row {1} is applicable.", ModuleId, 4);
        }
        if (Bomb.IsIndicatorOff(Indicator.BOB))
        {
            trueRows++;
            total += 5;
            Debug.LogFormat("[Two Knobs #{0}] Row {1} is applicable.", ModuleId, 5);
        }
        if (Bomb.IsIndicatorOn(Indicator.BOB))
        {
            trueRows++;
            total += 6;
            Debug.LogFormat("[Two Knobs #{0}] Row {1} is applicable.", ModuleId, 6);
        }
        if (Bomb.IsPortPresent(Port.Parallel))
        {
            trueRows++;
            total += 7;
            Debug.LogFormat("[Two Knobs #{0}] Row {1} is applicable.", ModuleId, 7);
        }
        if (Bomb.GetBatteryCount() == 0)
        {
            trueRows++;
            total += 8;
            Debug.LogFormat("[Two Knobs #{0}] Row {1} is applicable.", ModuleId, 8);
        }
        if (Bomb.GetPortPlateCount() >= 3)
        {
            trueRows++;
            total += 9;
            Debug.LogFormat("[Two Knobs #{0}] Row {1} is applicable.", ModuleId, 9);
        }
        if (trueRows > 0)
            cdKey = total / trueRows;
        else
            cdKey = 0;
    }
    int[] calculateBatteryTypeCount()
    {
        int[] output = new int[2];
        int totalBattCount = Bomb.GetBatteryCount();
        int totalHoldCount = Bomb.GetBatteryHolderCount();
        int dBattCount = 0;
        int aaBattCount = 0;
        if (totalBattCount == totalHoldCount)
        {
            dBattCount += totalHoldCount;
            totalBattCount = 0;
            totalHoldCount = 0;
        }
        else
        {
            while (totalBattCount > 0 && totalHoldCount > 0)
            {
                if (totalBattCount / totalHoldCount == 2.0)
                {
                    aaBattCount += totalHoldCount;
                    totalHoldCount = 0;
                    totalBattCount = 0;
                }
                else
                {
                    dBattCount++;
                    totalHoldCount--;
                    totalBattCount--;
                }
            }
        }
        output[0] = dBattCount;
        output[1] = aaBattCount;
        return output;
    }
    int DigitalRoot(int[] arr)
    {
        int sum = 0;
        for (int i = 0; i < arr.Length; i++)
            sum += arr[i];
        while (sum >= 10)
            sum = (sum % 10) + (sum / 10);
        return sum;
    }

    IEnumerator SlowRotate()
    {
        while (!ModuleSolved)
        {
            var framerate = 1f / Time.deltaTime; //Time.deltaTime = 2.5, framerate = 0.4
            var rotation = 12 / framerate; //30 seconds, change display every 2.5 seconds
            var y = knobG.transform.localEulerAngles.y;
            y += rotation;
            knobG.transform.localEulerAngles = new Vector3(0f, y, 0f);

            yield return null;
        }
    }
    void ChangeNumber()
    {
        displayedNum += interval;
        if (displayedNum == interval * 12 + initialDisplay)
            displayedNum = initialDisplay;
        displayNumber.text = displayedNum + "";
    }
    void ChangeNumberInput()
    {
        displayedNum++;
        if (displayedNum > 5)
            displayedNum = 1;
        displayNumber.text = displayedNum + "";
    }
}
