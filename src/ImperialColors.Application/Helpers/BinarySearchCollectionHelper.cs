namespace ImperialColors.Application.Helpers;

/// <summary>
/// Consultas O(log n) em coleções previamente ordenadas por chave numérica.
/// </summary>
public static class BinarySearchCollectionHelper
{
    public static List<T> OrdenarPorId<T>(IEnumerable<T> source, Func<T, int> getId)
        => source.OrderBy(getId).ToList();

    public static int FindIndexById<T>(IReadOnlyList<T> sortedById, int id, Func<T, int> getId)
    {
        if (sortedById.Count == 0)
            return -1;

        var lo = 0;
        var hi = sortedById.Count - 1;

        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) / 2);
            var midId = getId(sortedById[mid]);

            if (midId == id)
                return mid;

            if (midId < id)
                lo = mid + 1;
            else
                hi = mid - 1;
        }

        return -1;
    }

    public static T? FindById<T>(IReadOnlyList<T> sortedById, int id, Func<T, int> getId)
    {
        var index = FindIndexById(sortedById, id, getId);
        return index >= 0 ? sortedById[index] : default;
    }

    public static int FindIndexByKey<T, TKey>(IReadOnlyList<T> sortedByKey, TKey key, Func<T, TKey> getKey)
        where TKey : IComparable<TKey>
    {
        if (sortedByKey.Count == 0)
            return -1;

        var lo = 0;
        var hi = sortedByKey.Count - 1;

        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) / 2);
            var comparison = getKey(sortedByKey[mid]).CompareTo(key);

            if (comparison == 0)
                return mid;

            if (comparison < 0)
                lo = mid + 1;
            else
                hi = mid - 1;
        }

        return -1;
    }
}
