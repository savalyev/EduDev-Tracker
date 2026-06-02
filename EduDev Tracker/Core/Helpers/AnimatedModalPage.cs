using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Core.Helpers
{
    public class AnimatedModalPage : ContentPage
    {
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            this.TranslationY = 60;
            this.Opacity = 0;

            await Task.WhenAll(
                this.TranslateToAsync(0, 0, 280, Easing.CubicOut),
                this.FadeToAsync(1, 220, Easing.CubicOut)
            );
        }

        protected async Task CloseWithAnimation()
        {
            await Task.WhenAll(
                this.TranslateToAsync(0, 40, 200, Easing.CubicIn),
                this.FadeToAsync(0, 180, Easing.CubicIn)
            );
            await Navigation.PopModalAsync(animated: false);
        }
    }
}
