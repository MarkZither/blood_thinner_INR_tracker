namespace BloodThinnerTracker.Mobile.Views;

public partial class MainPage : ContentPage
{
    int count = 0;

    public MainPage()
    {
        InitializeComponent();
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        count++;

        if (sender is Button button)
        {
            if (count == 1)
                button.Text = $"Clicked {count} time";
            else
                button.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(button.Text);
        }
    }
}