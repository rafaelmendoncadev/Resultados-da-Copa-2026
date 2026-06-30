using Android.OS;
using AndroidX.AppCompat.App;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.Navigation;
using Resultados_da_Copa_2026.Fragments;
using Resultados_da_Copa_2026.Services;

namespace Resultados_da_Copa_2026;

[Activity(Label = "@string/app_name", MainLauncher = true, Theme = "@style/AppTheme", Exported = true)]
public class MainActivity : AppCompatActivity
{
    private GamesFragment? _gamesFragment;
    private StandingsFragment? _standingsFragment;
    private KnockoutFragment? _knockoutFragment;
    private AndroidX.Fragment.App.Fragment? _activeFragment;
    private TextView? _statusBanner;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);

        _statusBanner = FindViewById<TextView>(Resource.Id.statusBanner);

        _gamesFragment = new GamesFragment();
        _standingsFragment = new StandingsFragment();
        _knockoutFragment = new KnockoutFragment();

        _gamesFragment.DataLoaded += UpdateStatusBanner;
        _standingsFragment.DataLoaded += result => UpdateStatusBannerGeneric(result.IsOffline, result.FromCache, result.CachedAt);
        _knockoutFragment.DataLoaded += UpdateStatusBanner;

        if (savedInstanceState == null)
        {
            SupportFragmentManager.BeginTransaction()
                .Add(Resource.Id.fragmentContainer, _gamesFragment, "games")
                .Add(Resource.Id.fragmentContainer, _standingsFragment, "standings")
                .Hide(_standingsFragment)
                .Add(Resource.Id.fragmentContainer, _knockoutFragment, "knockout")
                .Hide(_knockoutFragment)
                .Commit();

            _activeFragment = _gamesFragment;
        }
        else
        {
            _gamesFragment = (GamesFragment)SupportFragmentManager.FindFragmentByTag("games")!;
            _standingsFragment = (StandingsFragment)SupportFragmentManager.FindFragmentByTag("standings")!;
            _knockoutFragment = (KnockoutFragment)SupportFragmentManager.FindFragmentByTag("knockout")!;
            _activeFragment = _gamesFragment;
        }

        var bottomNav = FindViewById<BottomNavigationView>(Resource.Id.bottomNavigation)!;
        bottomNav.SetOnItemSelectedListener(new BottomNavListener(this));
    }

    private void SwitchTab(AndroidX.Fragment.App.Fragment? target)
    {
        if (target == null || target == _activeFragment)
            return;

        SupportFragmentManager.BeginTransaction()
            .Hide(_activeFragment!)
            .Show(target)
            .Commit();

        _activeFragment = target;
    }

    private sealed class BottomNavListener(MainActivity activity) : Java.Lang.Object, NavigationBarView.IOnItemSelectedListener
    {
        public bool OnNavigationItemSelected(Android.Views.IMenuItem item)
        {
            var target = item.ItemId switch
            {
                Resource.Id.nav_games => (AndroidX.Fragment.App.Fragment?)activity._gamesFragment,
                Resource.Id.nav_standings => activity._standingsFragment,
                Resource.Id.nav_knockout => activity._knockoutFragment,
                _ => null
            };

            activity.SwitchTab(target);
            return true;
        }
    }

    private void UpdateStatusBanner(DataResult<List<Models.Game>> result) =>
        UpdateStatusBannerGeneric(result.IsOffline, result.FromCache, result.CachedAt);

    private void UpdateStatusBannerGeneric(bool isOffline, bool fromCache, DateTime? cachedAt)
    {
        if (_statusBanner == null)
            return;

        if (isOffline)
        {
            _statusBanner.Text = GetString(Resource.String.offline_mode);
            _statusBanner.Visibility = Android.Views.ViewStates.Visible;
            return;
        }

        if (fromCache && cachedAt.HasValue)
        {
            var localTime = cachedAt.Value.ToLocalTime().ToString("HH:mm");
            _statusBanner.Text = string.Format(GetString(Resource.String.last_update)!, localTime);
            _statusBanner.Visibility = Android.Views.ViewStates.Visible;
            return;
        }

        _statusBanner.Visibility = Android.Views.ViewStates.Gone;
    }
}
