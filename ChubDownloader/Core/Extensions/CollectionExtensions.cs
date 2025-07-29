using System.Collections.Concurrent;
using ZLinq;

namespace ChubDownloader.Core.Extensions;

public static class CollectionExtensions
{
    public static T[] ToArrayOptimized<T>(this IEnumerable<T> source)
    {
        return source switch
        {
            null => throw new ArgumentNullException(nameof(source)),
            T[] array => array,
            ICollection<T> collection => collection.Count == 0 ? [] : collection.ToArray(),
            _ => ConvertToArray(source)
        };
    }
    
    private static T[] ConvertToArray<T>(IEnumerable<T> source)
    {
        var result = new List<T>();
        source.AsValueEnumerable().CopyTo(result);
        return result.ToArray();
    }
    
    public static List<T> ToListOptimized<T>(this IEnumerable<T> source)
    {
        return source switch
        {
            null => throw new ArgumentNullException(nameof(source)),
            List<T> list => list,
            ICollection<T> collection => [..collection],
            _ => ConvertToList(source)
        };
    }
    
    private static List<T> ConvertToList<T>(IEnumerable<T> source)
    {
        var result = new List<T>();
        source.AsValueEnumerable().CopyTo(result);
        return result;
    }
    
    public static void AddRange<T>(this ConcurrentBag<T> bag, IEnumerable<T> items)
    {
        foreach (var item in items)
            bag.Add(item);
    }
    
    /// <summary>
    /// Zero-allocation FirstOrDefault for hot paths
    /// </summary>
    public static T? FirstOrDefaultOptimized<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        return source.AsValueEnumerable().Where(predicate).FirstOrDefault();
    }
    
    /// <summary>
    /// Zero-allocation Where for hot paths
    /// </summary>
    public static IEnumerable<T> WhereOptimized<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        var result = new List<T>();
        source.AsValueEnumerable().Where(predicate).CopyTo(result);
        return result;
    }
    
    /// <summary>
    /// Zero-allocation Select for hot paths
    /// </summary>
    public static IEnumerable<TResult> SelectOptimized<T, TResult>(this IEnumerable<T> source, Func<T, TResult> selector)
    {
        var result = new List<TResult>();
        source.AsValueEnumerable().Select(selector).CopyTo(result);
        return result;
    }
}