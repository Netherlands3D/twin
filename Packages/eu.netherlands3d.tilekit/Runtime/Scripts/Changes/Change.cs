using System;
using Netherlands3D.Tilekit.TileSets;
using RSG;
using UnityEngine;

namespace Netherlands3D.Tilekit.Changes
{
    public delegate Promise ChangeAction(Change change);

    public class Change
    {
        public Tile Tile { get; }

        public StatusOfChange Status { get; private set; }
        public TypeOfChange Type { get; }

        public bool IsPending => Status == StatusOfChange.Pending;
        public bool InProgress => Status == StatusOfChange.InProgress;
        public bool IsCancelled => Status == StatusOfChange.Cancelled;
        public bool IsCompleted => Status == StatusOfChange.Completed;


        // Events for state changes
        public Action<Change> Planned;
        public Action<Change> Triggered;
        public Action<Change> Cancelled;
        public Action<Change> Completed;

        public void Plan()
        {
            Planned?.Invoke(this);
        }
        
        public void Cancel()
        {
            Status = StatusOfChange.Cancelled;
            Cancelled?.Invoke(this);
        }

        public void Complete()
        {
            Status = StatusOfChange.Completed;
            Completed?.Invoke(this);
        }

        /// <summary>
        /// The requester is responsible for providing an action for this change to perform.
        /// The Change Plan system only tracks and reports on changes.
        /// </summary>
        private ChangeAction action = _ => Promise.Resolved() as Promise;

        private bool cancellable = false;

        public Change(TypeOfChange type, Tile tile)
        {
            this.Type = type;
            Tile = tile;
            Status = StatusOfChange.Pending;
        }

        public Promise Trigger()
        {
            Debug.Log("Triggered change: " + Status);
            // Can't trigger a triggered change
            if (!IsPending) return Promise.Rejected(new Exception("Trigger not pending")) as Promise;

            Debug.Log("Change is pending");

            Status = StatusOfChange.InProgress;
            Triggered?.Invoke(this);

            Debug.Log("Invoking change action");
            Debug.Log(action);

            var promise = action(this);
            return promise
                .Finally(Complete) as Promise;
        }

        public Tile[] AffectedTiles()
        {
            return new[] { Tile };
        }

        public Change UsingAction(ChangeAction action)
        {
            Debug.Log("Replace action");

            this.action = action;

            return this;
        }

        public static Change Add(Tile tile)
        {
            return new Change(TypeOfChange.Add, tile);
        }

        public static Change Remove(Tile tile)
        {
            return new Change(TypeOfChange.Remove, tile);
        }

        /// <summary>
        /// Some changes must play out and cannot be interrupted or cancelled, this means that any followup change
        /// should wait in the queue until this one completed.
        /// </summary>
        public void CannotBeCancelled()
        {
            cancellable = false;
        }
    }
}