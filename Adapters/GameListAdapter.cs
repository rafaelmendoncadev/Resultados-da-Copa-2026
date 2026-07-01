using Android.Views;
using AndroidX.RecyclerView.Widget;
using Resultados_da_Copa_2026.Models;
using Resultados_da_Copa_2026.Services;

namespace Resultados_da_Copa_2026.Adapters;

public class GameListItem
{
    public bool IsHeader { get; init; }
    public string? HeaderText { get; init; }
    public Game? Game { get; init; }
}

public class GameListAdapter : RecyclerView.Adapter
{
    private readonly List<GameListItem> _items = [];
    public event Action<Game>? ItemClick;

    public void SetItems(IEnumerable<GameListItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
        NotifyDataSetChanged();
    }

    public override int ItemCount => _items.Count;

    public override int GetItemViewType(int position) =>
        _items[position].IsHeader ? 0 : 1;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var inflater = LayoutInflater.From(parent.Context)!;
        if (viewType == 0)
        {
            var view = inflater.Inflate(Resource.Layout.item_section_header, parent, false)!;
            return new HeaderViewHolder(view);
        }

        var itemView = inflater.Inflate(Resource.Layout.item_game, parent, false)!;
        var gameHolder = new GameViewHolder(itemView);
        gameHolder.ItemView.Click += (_, _) =>
        {
            if (gameHolder.BoundGame != null)
                ItemClick?.Invoke(gameHolder.BoundGame);
        };
        return gameHolder;
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        var item = _items[position];
        if (holder is HeaderViewHolder headerHolder)
        {
            headerHolder.HeaderText.Text = item.HeaderText;
            return;
        }

        if (holder is GameViewHolder gameHolder && item.Game != null)
        {
            var game = item.Game;
            gameHolder.HomeTeamText.Text = GameDisplayHelper.GetHomeName(game);
            gameHolder.AwayTeamText.Text = GameDisplayHelper.GetAwayName(game);
            gameHolder.ScoreText.Text = GameDisplayHelper.FormatScore(game);
            gameHolder.DateText.Text = GameDisplayHelper.FormatDate(game.LocalDate, game.StadiumId);
            var stageText = game.Stage == MatchStage.Group
                ? $"Grupo {game.Group} · Rodada {game.Matchday}"
                : game.Stage.ToDisplayName();

            var qualification = GameDisplayHelper.GetQualificationText(game);
            if (!string.IsNullOrEmpty(qualification))
                stageText += $"\n{qualification}";

            gameHolder.StageText.Text = stageText;
            gameHolder.LiveBadge.Visibility = game.IsLive ? ViewStates.Visible : ViewStates.Gone;
            gameHolder.BoundGame = game;
        }
    }

    private class HeaderViewHolder(View itemView) : RecyclerView.ViewHolder(itemView)
    {
        public TextView HeaderText { get; } = itemView.FindViewById<TextView>(Resource.Id.headerText)!;
    }

    private class GameViewHolder(View itemView) : RecyclerView.ViewHolder(itemView)
    {
        public Game? BoundGame { get; set; }
        public TextView HomeTeamText { get; } = itemView.FindViewById<TextView>(Resource.Id.homeTeamText)!;
        public TextView AwayTeamText { get; } = itemView.FindViewById<TextView>(Resource.Id.awayTeamText)!;
        public TextView ScoreText { get; } = itemView.FindViewById<TextView>(Resource.Id.scoreText)!;
        public TextView DateText { get; } = itemView.FindViewById<TextView>(Resource.Id.dateText)!;
        public TextView StageText { get; } = itemView.FindViewById<TextView>(Resource.Id.stageText)!;
        public TextView LiveBadge { get; } = itemView.FindViewById<TextView>(Resource.Id.liveBadge)!;
    }
}
