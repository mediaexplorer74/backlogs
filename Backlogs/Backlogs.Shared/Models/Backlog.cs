﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace Backlogs.Models
{
    public class Backlog : INotifyPropertyChanged
    {
        public Guid id { get; set; }
        private string name;
        private string type;
        private string description;
        private string releaseDate;
        private string targetDate;
        private bool isComplete;
        private string imageURL;
        private string trailerURL;
        public int progress;
        private int length; 
        private string searchURL;
        private string units;
        private string director;
        private bool showProgress;
        private TimeSpan notifTime;
        private bool remindEveryday;
        private double userRating;
        private string createdDate;
        private string completedDate;

        public string Name
        {
            get => name;
            set
            {
                name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public string Type
        {
            get => type;
            set
            {
                type = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Type)));
            }
        }

        public string Description
        {
            get => description;
            set
            {
                description = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
            }
        }

        public string ReleaseDate
        {
            get => releaseDate;
            set
            {
                releaseDate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReleaseDate)));
            }
        }

        public string TargetDate
        {
            get => targetDate;
            set
            {
                targetDate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TargetDate)));
            }
        }

        public string Units
        {
            get => units;
            set
            {
                units = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Units)));
            }
        }

        public TimeSpan NotifTime
        {
            get => notifTime;
            set
            {
                notifTime = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NotifTime)));
            }
        }

        public bool IsComplete
        {
            get => isComplete;
            set
            {
                isComplete = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsComplete)));
            }
        }

        public bool ShowProgress
        {
            get => showProgress;
            set
            {
                showProgress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowProgress)));
            }
        }

        public bool RemindEveryday
        {
            get => remindEveryday;
            set
            {
                remindEveryday = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RemindEveryday)));
            }
        }

        public string ImageURL
        {
            get => imageURL;
            set
            {
                imageURL = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageURL)));
            }
        }

        public int Progress
        {
            get => progress;
            set
            {
                progress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progress)));
            }
        }

        public int Length
        {
            get => length;
            set
            {
                length = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Length)));
            }
        }

        public string TrailerURL
        {
            get => trailerURL;
            set
            {
                trailerURL = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrailerURL)));
            }
        }

        public string SearchURL
        {
            get => searchURL;
            set
            {
                searchURL = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchURL)));
            }
        }

        public string Director
        {
            get => director;
            set
            {
                director = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Director)));
            }
        }

        public double UserRating
        {
            get => userRating;
            set
            {
                userRating = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserRating)));
            }
        }

        public string CreatedDate
        {
            get => createdDate;
            set
            {
                createdDate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CreatedDate)));
            }
        }

        public string CompletedDate
        {
            get => completedDate;
            set
            {
                completedDate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompletedDate)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
