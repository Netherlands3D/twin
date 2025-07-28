namespace Netherlands3D.Catalogs
{
    /// <summary>
    /// A catalog whose contents can be modified in‐memory.
    /// </summary>
    public interface IWritableCatalog : ICatalog
    {
        /// <summary>Adds a new record to the catalog.</summary>
        void Add(Record record);

        /// <summary>Removes the record with the given Id. Returns true if something was removed.</summary>
        bool Remove(string id);

        /// <summary>Clears all records from the catalog.</summary>
        void Clear();
    }
}