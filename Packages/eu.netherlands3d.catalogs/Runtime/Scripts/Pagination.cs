using System;

namespace Netherlands3D.Catalogs
{
    public record Pagination
    {
        public int PageNumber => (int)(Offset / Limit) + 1;
        public int Offset { get; } = 0;
        public int Limit { get; } = 50;

        public Pagination(int offset = 0, int limit = 50)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "The offset must be greater than or equal to 0.");
            }

            if (limit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), "The limit must be more than or equal to 1.");
            }
            if (limit > 1000)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), "The limit must be less than or equal to 1000.");
            }

            this.Offset = offset;
            this.Limit = limit;
        }
        
        public static Pagination WithOffset(int offset, int limit)
        {
            return new Pagination(offset, limit);
        }

        public static Pagination WithPageNumber(int pageNumber, int limit)
        {
            return new Pagination((pageNumber - 1) * limit, limit);
        }

        public Pagination Next()
        {
            return new Pagination(Math.Clamp(Offset + Limit, 0, int.MaxValue), Limit);
        }

        public Pagination Previous()
        {
            return new Pagination(Math.Clamp(Offset - Limit, 0, int.MaxValue), Limit);
        }
    }
}