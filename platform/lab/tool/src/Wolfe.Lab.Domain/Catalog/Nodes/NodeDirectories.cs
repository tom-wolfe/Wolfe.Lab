using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Domain.Catalog.Nodes;

/// <summary>
/// Important places on the node.
/// </summary>
/// <param name="Root">Where components and their artifacts are installed: <c>{lab.root}</c>.</param>
/// <param name="Data">Where services keep their state: <c>{lab.data}</c>.</param>
public sealed record NodeDirectories(AbsolutePath Root, AbsolutePath Data);
