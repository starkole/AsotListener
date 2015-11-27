namespace AsotListener.Services.Navigation
{
    /// <summary>
    /// Represents the method that will handle the <see cref="Services.NavigationHelper.LoadState"/>event
    /// </summary>
    public delegate void LoadStateEventHandler(object sender, LoadStateEventArgs e);

    /// <summary>
    /// Represents the method that will handle the <see cref="Services.NavigationHelper.SaveState"/>event
    /// </summary>
    public delegate void SaveStateEventHandler(object sender, SaveStateEventArgs e);
}
