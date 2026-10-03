namespace Wolfe.Lab.Infrastructure.Checkout;

/// <summary>
/// The well-known problems with where a job runs, in the repository.
/// </summary>
public static class RepositoryErrors
{
    /// <summary>
    /// The directory is in no git checkout.
    /// </summary>
    public static Error NotInARepository(IDirectory directory) => new($"{directory.AbsolutePath} is not in a git checkout.");

    /// <summary>
    /// The directory is not in the checkout git names.
    /// </summary>
    public static Error OutsideTheCheckout(IDirectory directory, IDirectory checkout) =>
        new($"{directory.AbsolutePath} is not in the checkout at {checkout.AbsolutePath}.");
}
