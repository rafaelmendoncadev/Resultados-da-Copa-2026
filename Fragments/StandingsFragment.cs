using Android.OS;
using Android.Views;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using AndroidX.SwipeRefreshLayout.Widget;
using Resultados_da_Copa_2026.Adapters;
using Resultados_da_Copa_2026.Models;
using Resultados_da_Copa_2026.Services;

namespace Resultados_da_Copa_2026.Fragments;

public class StandingsFragment : AndroidX.Fragment.App.Fragment
{
    private MatchRepository? _repository;
    private GroupStandingAdapter? _adapter;
    private SwipeRefreshLayout? _swipeRefresh;
    private RecyclerView? _recyclerView;
    private ProgressBar? _progressBar;
    private View? _errorLayout;

    public event Action<DataResult<List<GroupStanding>>>? DataLoaded;

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        return inflater.Inflate(Resource.Layout.fragment_standings, container, false);
    }

    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);
        _repository = new MatchRepository(RequireContext());
        _swipeRefresh = view.FindViewById<SwipeRefreshLayout>(Resource.Id.swipeRefresh);
        _recyclerView = view.FindViewById<RecyclerView>(Resource.Id.recyclerView);
        _progressBar = view.FindViewById<ProgressBar>(Resource.Id.progressBar);
        _errorLayout = view.FindViewById(Resource.Id.errorLayout);

        _adapter = new GroupStandingAdapter();
        _recyclerView!.SetLayoutManager(new LinearLayoutManager(RequireContext()));
        _recyclerView.SetAdapter(_adapter);

        _swipeRefresh!.SetColorSchemeResources(Resource.Color.wc_green_dark);
        _swipeRefresh.Refresh += async (_, _) => await LoadDataAsync(forceRefresh: true);

        view.FindViewById<Google.Android.Material.Button.MaterialButton>(Resource.Id.retryButton)!
            .Click += async (_, _) => await LoadDataAsync(forceRefresh: true);

        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync(bool forceRefresh = false)
    {
        ShowLoading(true);
        try
        {
            var result = await _repository!.GetGroupsAsync(RequireContext(), forceRefresh);
            _adapter!.SetGroups(result.Data);
            DataLoaded?.Invoke(result);
            ShowError(result.Data.Count == 0);
        }
        catch
        {
            ShowError(true);
        }
        finally
        {
            ShowLoading(false);
            _swipeRefresh!.Refreshing = false;
        }
    }

    private void ShowLoading(bool show)
    {
        _progressBar!.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
        if (show)
            _errorLayout!.Visibility = ViewStates.Gone;
    }

    private void ShowError(bool show)
    {
        _errorLayout!.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
        _recyclerView!.Visibility = show ? ViewStates.Gone : ViewStates.Visible;
    }
}
