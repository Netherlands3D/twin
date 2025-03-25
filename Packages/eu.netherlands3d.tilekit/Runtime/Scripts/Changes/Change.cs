using System;
using Netherlands3D.Tilekit.TileSets;
using RSG;

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

        public Change(TypeOfChange type, Tile tile)
        {
            this.Type = type;
            Tile = tile;
            Status = StatusOfChange.Pending;
        }

        public Promise Trigger()
        {
            // Can't trigger a triggered change
            if (!IsPending) return null;

            Status = StatusOfChange.InProgress;
            Triggered?.Invoke(this);

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
    }
}