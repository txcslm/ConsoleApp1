using System.Collections.Concurrent;

namespace ChubDownloader.Core.Extensions;

public static class CollectionExtensions
{
    public static T[] ToArrayOptimized<T>(this IEnumerable<T> source)
    {
        return source switch
        {
            null => throw new ArgumentNullException(nameof(source)),
            T[] array => array,
            ICollection<T> collection => collection.Count == 0 ? Array.Empty<T>() : collection.ToArray(),
            _ => source.ToArray()
        };
    }
    
    public static List<T> ToListOptimized<T>(this IEnumerable<T> source)
    {
        return source switch
        {
            null => throw new ArgumentNullException(nameof(source)),
            List<T> list => list,
            ICollection<T> collection => new List<T>(collection),
            _ => source.ToList()
        };
    }
    
    public static void AddRange<T>(this ConcurrentBag<T> bag, IEnumerable<T> items)
    {
        foreach (var item in items)
            bag.Add(item);
    }
}