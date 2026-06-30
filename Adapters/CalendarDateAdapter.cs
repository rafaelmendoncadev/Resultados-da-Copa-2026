using Android.Content;
using Android.Views;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Card;

namespace Resultados_da_Copa_2026.Adapters;

public class CalendarDateItem
{
    /// <summary>
    /// Chave da data no formato "dd/MM", ou null para "Todas as datas".
    /// </summary>
    public string? DateKey { get; init; }
    public string? DayName { get; init; }               // "Sáb", "Dom" etc.
    public string DayNumber { get; init; } = "";         // "04"
    public string? Month { get; init; }                  // "Jul"
    public bool HasGames { get; init; }
    public bool IsSelected { get; set; }
}

public class CalendarDateAdapter : RecyclerView.Adapter
{
    private readonly List<CalendarDateItem> _dates = [];
    private readonly Context _context;

    private int _colorGreenDark;
    private int _colorCard;
    private int _colorTextPrimary;
    private int _colorTextSecondary;
    private int _colorWhite;

    public event Action<CalendarDateItem>? DateClick;

    public CalendarDateAdapter(Context context)
    {
        _context = context;
        ResolveColors();
    }

    private void ResolveColors()
    {
        _colorGreenDark = ContextCompat.GetColor(_context, Resource.Color.wc_green_dark);
        _colorCard = ContextCompat.GetColor(_context, Resource.Color.wc_card);
        _colorTextPrimary = ContextCompat.GetColor(_context, Resource.Color.wc_text_primary);
        _colorTextSecondary = ContextCompat.GetColor(_context, Resource.Color.wc_text_secondary);
        _colorWhite = global::Android.Graphics.Color.White;
    }

    public void SetDates(IEnumerable<CalendarDateItem> dates)
    {
        _dates.Clear();
        _dates.AddRange(dates);
        NotifyDataSetChanged();
    }

    public void UpdateSelection(string? selectedKey)
    {
        foreach (var item in _dates)
            item.IsSelected = item.DateKey == selectedKey;
        NotifyDataSetChanged();
    }

    public override int ItemCount => _dates.Count;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var inflater = LayoutInflater.From(parent.Context)!;
        var view = inflater.Inflate(Resource.Layout.item_calendar_date, parent, false)!;
        return new CalendarDateViewHolder(view);
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        var item = _dates[position];
        var vh = (CalendarDateViewHolder)holder;
        var context = vh.ItemView.Context;

        vh.DayNameText.Text = item.DayName;
        vh.DayNumberText.Text = item.DayNumber;
        vh.MonthText.Text = item.Month;
        vh.GameDot.Visibility = item.HasGames ? ViewStates.Visible : ViewStates.Gone;

        // Aplica cor de fundo e texto conforme seleção
        var card = vh.ItemView as MaterialCardView;
        if (card != null)
        {
            if (item.IsSelected)
            {
                card.SetCardBackgroundColor(_colorGreenDark);
                vh.DayNameText.SetTextColor(new Android.Graphics.Color(_colorWhite));
                vh.DayNumberText.SetTextColor(new Android.Graphics.Color(_colorWhite));
                vh.MonthText.SetTextColor(global::Android.Graphics.Color.Argb(180, 255, 255, 255));
                card.StrokeWidth = 0;
            }
            else
            {
                card.SetCardBackgroundColor(_colorCard);
                vh.DayNameText.SetTextColor(new Android.Graphics.Color(_colorTextSecondary));
                vh.DayNumberText.SetTextColor(new Android.Graphics.Color(_colorTextPrimary));
                vh.MonthText.SetTextColor(new Android.Graphics.Color(_colorTextSecondary));
                card.StrokeWidth = 0;
            }
        }

        vh.ItemView.Click -= OnItemClick;
        vh.ItemView.Click += OnItemClick;

        void OnItemClick(object? sender, EventArgs e)
        {
            DateClick?.Invoke(item);
        }
    }

    private class CalendarDateViewHolder(View itemView) : RecyclerView.ViewHolder(itemView)
    {
        public TextView DayNameText { get; } = itemView.FindViewById<TextView>(Resource.Id.dayNameText)!;
        public TextView DayNumberText { get; } = itemView.FindViewById<TextView>(Resource.Id.dayNumberText)!;
        public TextView MonthText { get; } = itemView.FindViewById<TextView>(Resource.Id.monthText)!;
        public View GameDot { get; } = itemView.FindViewById(Resource.Id.gameDot)!;
    }
}
