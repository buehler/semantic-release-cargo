using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace SemanticReleaseCargoTests;

public static class Helpers
{
    public static FSharpAsync<T> AsAsync<T>(this T value) => FSharpAsync.AwaitTask(Task.FromResult(value));

    public static Task<T> Run<T>(this FSharpAsync<T> async) => FSharpAsync.StartAsTask(
        async,
        FSharpOption<TaskCreationOptions>.None,
        FSharpOption<CancellationToken>.None);

    public static FSharpMap<TKey, TValue> AsMap<TKey, TValue>(this Dictionary<TKey, TValue> dict) where TKey : notnull =>
        MapModule.OfSeq(dict.Select(kv => Tuple.Create(kv.Key, kv.Value)));
}
