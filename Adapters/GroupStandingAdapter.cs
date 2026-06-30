using Android.Views;
using AndroidX.RecyclerView.Widget;
using Resultados_da_Copa_2026.Models;

namespace Resultados_da_Copa_2026.Adapters;

public class GroupStandingAdapter : RecyclerView.Adapter
{
    private readonly List<GroupStanding> _groups = [];

    public void SetGroups(IEnumerable<GroupStanding> groups)
    {
        _groups.Clear();
        _groups.AddRange(groups.OrderBy(g => g.Name));
        NotifyDataSetChanged();
    }

    public override int ItemCount => _groups.Count;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var view = LayoutInflater.From(parent.Context)!
            .Inflate(Resource.Layout.item_group_standing, parent, false)!;
        return new GroupViewHolder(view);
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is not GroupViewHolder groupHolder)
            return;

        var group = _groups[position];
        groupHolder.GroupTitle.Text = $"Grupo {group.Name}";
        groupHolder.StandingsContainer.RemoveAllViews();

        var inflater = LayoutInflater.From(groupHolder.ItemView.Context)!;
        for (var i = 0; i < group.Teams.Count; i++)
        {
            var entry = group.Teams[i];
            var rowView = inflater.Inflate(Resource.Layout.item_standing_row, groupHolder.StandingsContainer, false)!;
            rowView.FindViewById<TextView>(Resource.Id.positionText)!.Text = (i + 1).ToString();
            rowView.FindViewById<TextView>(Resource.Id.teamText)!.Text = entry.TeamName;
            rowView.FindViewById<TextView>(Resource.Id.mpText)!.Text = entry.MatchesPlayed;
            rowView.FindViewById<TextView>(Resource.Id.gdText)!.Text = entry.GoalDifference;
            rowView.FindViewById<TextView>(Resource.Id.ptsText)!.Text = entry.Points;
            groupHolder.StandingsContainer.AddView(rowView);
        }
    }

    private class GroupViewHolder(View itemView) : RecyclerView.ViewHolder(itemView)
    {
        public TextView GroupTitle { get; } = itemView.FindViewById<TextView>(Resource.Id.groupTitle)!;
        public LinearLayout StandingsContainer { get; } = itemView.FindViewById<LinearLayout>(Resource.Id.standingsContainer)!;
    }
}
