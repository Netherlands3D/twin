using System;
using System.Collections.Generic;
using System.Linq;

namespace Netherlands3D.Twin.DataSets
{
    public sealed class DashboardData
    {
        public int SourceRowCount { get; }
        public int SkippedRowCount => SkippedLineNumbers.Count;
        public IReadOnlyList<int> SkippedLineNumbers { get; }
        public IReadOnlyList<HousingRow> Housing { get; }
        public IReadOnlyList<NonHousingRow> NonHousing { get; }
        public IReadOnlyList<ProgramChangeRow> ProgramChanges { get; }

        public DashboardData(IReadOnlyList<HousingRow> housing, IReadOnlyList<NonHousingRow> nonHousing, IReadOnlyList<ProgramChangeRow> programChanges)
            : this(housing, nonHousing, programChanges, Array.Empty<int>())
        {
        }

        internal DashboardData(IReadOnlyList<HousingRow> housing, IReadOnlyList<NonHousingRow> nonHousing, IReadOnlyList<ProgramChangeRow> programChanges, IReadOnlyList<int> skippedLineNumbers)
        {
            SkippedLineNumbers = skippedLineNumbers.ToArray();
            SourceRowCount = housing.Count + nonHousing.Count + programChanges.Count + SkippedRowCount;
            Housing = housing.Where(row => !string.IsNullOrWhiteSpace(row.Block) && !string.IsNullOrWhiteSpace(row.Phase) &&
                !string.IsNullOrWhiteSpace(row.Segment) && row.Rooms.HasValue && row.DwellingCount > 0).ToArray();
            NonHousing = nonHousing.Where(row => !string.IsNullOrWhiteSpace(row.Block) && !string.IsNullOrWhiteSpace(row.Phase) &&
                !string.IsNullOrWhiteSpace(row.Category) && row.GrossFloorArea > 0).ToArray();
            ProgramChanges = programChanges.Where(row => !string.IsNullOrWhiteSpace(row.Block) && !string.IsNullOrWhiteSpace(row.Phase) &&
                !string.IsNullOrWhiteSpace(row.Deviation)).ToArray();
        }

        public sealed class HousingRow
        {
            public string Block { get; }
            public string Phase { get; }
            public DateTime? SubmissionDate { get; }
            public string Segment { get; }
            public int? Rooms { get; }
            public decimal? MinimumUsableFloorArea { get; }
            public int? DwellingCount { get; }

            public HousingRow(string block, string phase, DateTime? submissionDate, string segment, int? rooms, decimal? minimumUsableFloorArea, int? dwellingCount)
            {
                Block = block;
                Phase = phase;
                SubmissionDate = submissionDate;
                Segment = segment;
                Rooms = rooms;
                MinimumUsableFloorArea = minimumUsableFloorArea;
                DwellingCount = dwellingCount;
            }
        }

        public sealed class NonHousingRow
        {
            public string Block { get; }
            public string Phase { get; }
            public string Category { get; }
            public decimal? GrossFloorArea { get; }

            public NonHousingRow(string block, string phase, string category, decimal? grossFloorArea)
            {
                Block = block;
                Phase = phase;
                Category = category;
                GrossFloorArea = grossFloorArea;
            }
        }

        public sealed class ProgramChangeRow
        {
            public string Block { get; }
            public string Phase { get; }
            public string Program { get; }
            public string Deviation { get; }

            public ProgramChangeRow(string block, string phase, string program, string deviation)
            {
                Block = block;
                Phase = phase;
                Program = program;
                Deviation = deviation;
            }
        }
    }
}
