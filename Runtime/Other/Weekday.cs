using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Yurowm {
    [Flags]
    public enum Weekday {
        OddMonday = 1 << 0,
        OddTuesday = 1 << 1,
        OddWednesday = 1 << 2,
        OddThursday = 1 << 3,
        OddFriday = 1 << 4,
        OddSaturday = 1 << 5,
        OddSunday = 1 << 6,
        EvenMonday = 1 << 7,
        EvenTuesday = 1 << 8,
        EvenWednesday = 1 << 9,
        EvenThursday = 1 << 10,
        EvenFriday = 1 << 11,
        EvenSaturday = 1 << 12,
        EvenSunday = 1 << 13,
        All = 0b1111111_1111111
    }
    
    public static class Weekdays {
        
        static readonly DateTime BaseDate = DateTime.MinValue.AddDays(6);
        
        public static Weekday GetWeekday(this DateTime date) {
            var even = ((float) (date.Date - BaseDate).TotalDays / 7).CeilToInt() % 2 == 0;

            switch (date.DayOfWeek) {
                case DayOfWeek.Monday: return even ? Weekday.EvenMonday : Weekday.OddMonday;
                case DayOfWeek.Tuesday: return even ? Weekday.EvenTuesday : Weekday.OddTuesday;
                case DayOfWeek.Wednesday: return even ? Weekday.EvenWednesday : Weekday.OddWednesday;
                case DayOfWeek.Thursday: return even ? Weekday.EvenThursday : Weekday.OddThursday;
                case DayOfWeek.Friday: return even ? Weekday.EvenFriday : Weekday.OddFriday;
                case DayOfWeek.Saturday: return even ? Weekday.EvenSaturday : Weekday.OddSaturday;
                case DayOfWeek.Sunday: return even ? Weekday.EvenSunday : Weekday.OddSunday;
            }
            
            return 0;
        }
    }
}