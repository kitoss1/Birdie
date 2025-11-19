using System;
using UnityEngine;

namespace Birdie.Data
{
    [Serializable]
    public sealed class TimeRange
    {
        [SerializeField]
        [Range(0, 23)]
        private int m_startHour = 8;

        [SerializeField]
        [Range(0, 23)]
        private int m_endHour = 18;

        public int StartHour
        {
            get => m_startHour;
            set => m_startHour = value;
        }

        public int EndHour
        {
            get => m_endHour;
            set => m_endHour = value;
        }

        /// <summary>
        /// Checks if a given hour is within this time range
        /// Handles wrap-around (e.g., 22:00 to 4:00)
        /// </summary>
        public bool IsTimeInRange(int hour)
        {
            if (StartHour <= EndHour)
            {
                // Normal range (e.g., 8 to 18)
                return hour >= StartHour && hour <= EndHour;
            }
            else
            {
                // Wrap-around range (e.g., 22 to 4)
                return hour >= StartHour || hour <= EndHour;
            }
        }
    }
}

