namespace albums_api.Models
{
    public record Album(int Id, string Title, Artist Artist, double Price, string Image_url, int Year)
    {
        // In-memory store. Seeded once; mutated by Add/Update/Delete.
        private static readonly List<Album> _albums = new()
        {
            new Album(1, "You, Me and an App Id", new Artist("Daprize", new DateOnly(1985, 4, 12), "Seattle, USA"), 10.99, "https://aka.ms/albums-daprlogo", 2021),
            new Album(2, "Seven Revision Army", new Artist("The Blue-Green Stripes", new DateOnly(1978, 9, 30), "Dublin, Ireland"), 13.99, "https://aka.ms/albums-containerappslogo", 2022),
            new Album(3, "Scale It Up", new Artist("KEDA Club", new DateOnly(1990, 1, 22), "Amsterdam, Netherlands"), 13.99, "https://aka.ms/albums-kedalogo", 2022),
            new Album(4, "Lost in Translation", new Artist("MegaDNS", new DateOnly(1982, 6, 5), "Tokyo, Japan"), 12.99, "https://aka.ms/albums-envoylogo", 2023),
            new Album(5, "Lock Down Your Love", new Artist("V is for VNET", new DateOnly(1995, 11, 17), "Berlin, Germany"), 12.99, "https://aka.ms/albums-vnetlogo", 2023),
            new Album(6, "Sweet Container O' Mine", new Artist("Guns N Probeses", new DateOnly(1988, 3, 8), "Los Angeles, USA"), 14.99, "https://aka.ms/albums-containerappslogo", 2024)
        };

        private static readonly object _lock = new();

        public static List<Album> GetAll()
        {
            lock (_lock) { return _albums.ToList(); }
        }

        public static Album? GetById(int id)
        {
            lock (_lock) { return _albums.FirstOrDefault(a => a.Id == id); }
        }

        public static List<Album> GetByYear(int year)
        {
            lock (_lock) { return _albums.Where(a => a.Year == year).ToList(); }
        }

        public static Album Add(Album album)
        {
            lock (_lock)
            {
                var nextId = _albums.Count == 0 ? 1 : _albums.Max(a => a.Id) + 1;
                var created = album with { Id = nextId };
                _albums.Add(created);
                return created;
            }
        }

        public static Album? Update(int id, Album album)
        {
            lock (_lock)
            {
                var index = _albums.FindIndex(a => a.Id == id);
                if (index < 0) return null;
                var updated = album with { Id = id };
                _albums[index] = updated;
                return updated;
            }
        }

        public static bool Delete(int id)
        {
            lock (_lock) { return _albums.RemoveAll(a => a.Id == id) > 0; }
        }
    }
}
