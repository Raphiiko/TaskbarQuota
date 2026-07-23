using System;
using System.Collections.Generic;
using TaskbarQuota.Usage;

namespace TaskbarQuota.Tests;

public class WidgetDisplayProvidersTests
{
    // Enum order used for the "pinned in enum order" expectations.
    private static readonly IReadOnlyList<ProviderId> Ordered =
        new[] { ProviderId.Claude, ProviderId.Codex, ProviderId.Cursor };

    private static IReadOnlyList<ProviderId> Compute(
        ProviderId? active,
        bool present,
        ProviderId[] pinned,
        ProviderId[]? invisible = null,
        ProviderId[]? unavailable = null,
        ProviderId? fallback = null)
    {
        var pinnedSet = new HashSet<ProviderId>(pinned);
        var invisibleSet = new HashSet<ProviderId>(invisible ?? Array.Empty<ProviderId>());
        var unavailableSet = new HashSet<ProviderId>(unavailable ?? Array.Empty<ProviderId>());
        return UsageCoordinator.ComputeWidgetDisplayProviders(
            active,
            present,
            Ordered,
            pinnedSet.Contains,
            id => !invisibleSet.Contains(id),
            id => !unavailableSet.Contains(id),
            fallback);
    }

    [Fact]
    public void PinnedShownEvenWhenNoToolPresent()
        => Assert.Equal(
            new[] { ProviderId.Claude, ProviderId.Codex },
            Compute(active: null, present: false, pinned: new[] { ProviderId.Claude, ProviderId.Codex }));

    [Fact]
    public void PinnedOrderFollowsEnumOrder_RegardlessOfPinArgOrder()
        => Assert.Equal(
            new[] { ProviderId.Claude, ProviderId.Codex },
            Compute(active: null, present: false, pinned: new[] { ProviderId.Codex, ProviderId.Claude }));

    [Fact]
    public void NonPinnedActiveGoesFirst_ThenPinned()
        => Assert.Equal(
            new[] { ProviderId.Cursor, ProviderId.Claude, ProviderId.Codex },
            Compute(active: ProviderId.Cursor, present: true, pinned: new[] { ProviderId.Claude, ProviderId.Codex }));

    [Fact]
    public void PinnedActiveShownOnce_InPinnedPosition()
        => Assert.Equal(
            new[] { ProviderId.Claude, ProviderId.Codex },
            Compute(active: ProviderId.Codex, present: true, pinned: new[] { ProviderId.Claude, ProviderId.Codex }));

    [Fact]
    public void ActiveIgnoredWhenNoToolPresent()
        => Assert.Equal(
            new[] { ProviderId.Claude },
            Compute(active: ProviderId.Cursor, present: false, pinned: new[] { ProviderId.Claude }));

    [Fact]
    public void UnavailablePinIsFilteredOut()
        => Assert.Equal(
            new[] { ProviderId.Codex },
            Compute(active: null, present: false, pinned: new[] { ProviderId.Claude, ProviderId.Codex },
                unavailable: new[] { ProviderId.Claude }));

    [Fact]
    public void InvisiblePinIsFilteredOut()
        => Assert.Equal(
            new[] { ProviderId.Codex },
            Compute(active: null, present: false, pinned: new[] { ProviderId.Claude, ProviderId.Codex },
                invisible: new[] { ProviderId.Claude }));

    [Fact]
    public void FallbackUsedOnlyWhenNothingElseAndPresent()
    {
        Assert.Equal(
            new[] { ProviderId.Cursor },
            Compute(active: null, present: true, pinned: Array.Empty<ProviderId>(), fallback: ProviderId.Cursor));

        // No tool present and nothing pinned -> empty (widget hides), fallback ignored.
        Assert.Empty(
            Compute(active: null, present: false, pinned: Array.Empty<ProviderId>(), fallback: ProviderId.Cursor));
    }
}
