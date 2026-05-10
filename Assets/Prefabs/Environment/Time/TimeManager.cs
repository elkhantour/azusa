using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public enum DayState
{
    SUNRISE,
    DAY,
    SUNSET,
    NIGHT
}

public class DayMoment
{
    public DayState state;
    public float startTime;
    public float transitionTime;
}

public class DayTime
{
    public float hours;
    public float minutes;
    public float seconds;
    public float length;
}


public class TimeManager : MonoBehaviour
{
    private static TimeManager _instance;
    public static TimeManager Instance
    {
        get
        {
            return _instance;
        }
    }

    //Time
    [SerializeField]
    [Header("Time")]
    public bool _runTime = true;
    public float _dayTotalTime = 24f; //length of a day in minute
    public float _timeSpeed = 1f;
    private DayTime time = new DayTime { hours = 0f, minutes = 0f, seconds = 0f };
    public static event Action<int, DayMoment> OnUpdateMoment;

    //Cycle
    [Space]
    private DayMoment[] _dayMoments;

    private int _currentMomentIndex = -1;
    protected static DayState _currentDayState;

    private void updateTime()
    {

        //Update Global Hour an Minutes
        time.seconds += Time.deltaTime * _timeSpeed;
        if (time.seconds >= time.length)
        {
            time.seconds = 0;
        }

        time.hours = Mathf.FloorToInt(time.seconds / 60f);
        time.minutes = Mathf.FloorToInt(time.seconds % 60f);

        //Debug.Log(time.hours + ":" + time.minutes + ":" + time.seconds);

        //update day moment depending on current time percentage
        int numberDays = _dayMoments.Length;
        for (int d = 0; d < numberDays; d++)
        {
            DayMoment currentMoment = _dayMoments[d];
            DayMoment nextMoment = _dayMoments[(d + 1) % _dayMoments.Length];
            if (((d < numberDays - 1 && time.hours >= currentMoment.startTime && time.hours < nextMoment.startTime) ||
                 (d == numberDays - 1 && time.hours >= currentMoment.startTime)) &&
                _currentMomentIndex != d)
            {
                _currentMomentIndex = d;
                OnUpdateMoment?.Invoke(d, currentMoment);
            }
        }
    }

    void Awake()
    {
        _instance = this;
        enabled = _runTime;
    }


    void Start()
    {

        //Start Cycle
        time.length = _dayTotalTime * 60f;

        //Set days Moments
        _dayMoments = new DayMoment[] {
            new DayMoment { state = DayState.SUNRISE, startTime = 1f, transitionTime = 10f / _timeSpeed },
            new DayMoment { state = DayState.DAY, startTime = 2f, transitionTime = 10f / _timeSpeed },
            new DayMoment { state = DayState.SUNSET, startTime = 3f, transitionTime = 10f / _timeSpeed },
            new DayMoment { state = DayState.NIGHT, startTime = 4f, transitionTime = 10f / _timeSpeed }
        };
    }

    void Update()
    {
        updateTime();
    }
}
