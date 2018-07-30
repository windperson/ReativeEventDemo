using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ReativeEventDemo
{
    public partial class MainPage : ContentPage
    {
        private IDisposable _subscribe;

        public MainPage()
        {
            InitializeComponent();
        }

        private void MainPage_OnAppearing(object sender, EventArgs e)
        {
            _subscribe = SetupEventStream().Subscribe(color =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Console.WriteLine("color changed");
                    ColorCanvas.Color = color;
                });
            });
        }

        private void MainPage_OnDisappearing(object sender, EventArgs e)
        {
            _subscribe?.Dispose();
        }

        private IObservable<Color> SetupEventStream()
        {
            var redSliderStream = GetSliderEventStream(RedSlider);
            var greenSliderStream = GetSliderEventStream(BlueSlider);
            var blueSliderStream = GetSliderEventStream(BlueSlider);
            var alphaSliderStream = GetSliderEventStream(AlphaSlider);

            var redEntryStream = GetEntryEventStream(RedEntry);
            var greenEntryStream = GetEntryEventStream(GreenEntry);
            var blueEntryStream = GetEntryEventStream(BlueEntry);
            var alphaEntryStream = GetEntryEventStream(AlphaEntry);

            var redValueStream = redSliderStream.Merge(redEntryStream);
            var greenValueStream = greenSliderStream.Merge(greenEntryStream);
            var blueValueStream = blueSliderStream.Merge(blueEntryStream);
            var alphaValueStream = alphaSliderStream.Merge(alphaEntryStream);

            return alphaValueStream.Zip(redValueStream, greenValueStream, blueValueStream,
                (alpha, red, green, blue) =>
                {
                    Console.WriteLine("zip color parameters");
                    return Color.FromRgba(red, green, blue, alpha);
                });
        }

        private IObservable<double> GetSliderEventStream(Slider slider)
        {
            return Observable.FromEventPattern<EventHandler<ValueChangedEventArgs>, ValueChangedEventArgs>(ev => slider.ValueChanged += ev,
                ev => slider.ValueChanged -= ev).Select(x => x.EventArgs.NewValue).Throttle(new TimeSpan(0, 0, 1));
        }

        private IObservable<double> GetEntryEventStream(Entry entry)
        {
            return Observable.FromEventPattern<EventHandler<TextChangedEventArgs>, TextChangedEventArgs>(ev => entry.TextChanged += ev,
                ev => entry.TextChanged -= ev).Select(x =>
            {
                var value = x.EventArgs.NewTextValue;
                if (double.TryParse(value, out double ret))
                {
                    return ret;
                }
                return 0;
            }).Throttle(new TimeSpan(0, 0, 3));
        }
    }
}
