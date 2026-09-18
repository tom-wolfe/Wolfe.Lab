using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Wolfe.Lab.Build;

/// <summary>
/// Opens a <see cref="Result{T}"/> as the two outcomes it is, so a caller holds a value or
/// errors and never a maybe.
/// </summary>
public static class Results
{
    extension<T>(Result<T> result) where T : class
    {
        /// <summary>
        /// The value, when the result succeeded; the errors otherwise.
        /// </summary>
        /// <param name="value">The value, when there is one.</param>
        /// <param name="errors">The errors, when there is no value.</param>
        public bool TryGetValue([NotNullWhen(true)] out T? value, [NotNullWhen(false)] out ReadOnlyCollection<Error>? errors)
        {
            value = result.Value;
            errors = result.Errors;
            if (value is not null && errors is null)
            {
                return true;
            }

            // A result built with neither is a programming error, reported rather than hidden.
            errors ??= new ReadOnlyCollection<Error>([new Error("The result carries neither a value nor errors.")]);
            value = null;
            return false;
        }
    }
}
