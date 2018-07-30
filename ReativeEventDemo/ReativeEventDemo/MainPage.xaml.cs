using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public MainPage()
        {
            InitializeComponent();

            SetupEventStream();
        }

        ~MainPage()
        {
            _subscriptions.ForEach(sub => sub.Dispose());
        }


        private void SetupEventStream()
        {
            var redSliderStream = GetSliderEventStream(RedSlider, RedSlider.Minimum);
            _subscriptions.Add(redSliderStream.Subscribe(UpdateEntry(RedEntry)));

            var greenSliderStream = GetSliderEventStream(GreenSlider, GreenSlider.Minimum);
            _subscriptions.Add(greenSliderStream.Subscribe(UpdateEntry(GreenEntry)));

            var blueSliderStream = GetSliderEventStream(BlueSlider, BlueSlider.Minimum);
            _subscriptions.Add(blueSliderStream.Subscribe(UpdateEntry(BlueEntry)));

            var alphaSliderStream = GetSliderEventStream(AlphaSlider, AlphaSlider.Maximum);
            _subscriptions.Add(alphaSliderStream.Subscribe(UpdateEntry(AlphaEntry)));

            var redEntryStream = GetEntryEventStream(RedEntry);
            _subscriptions.Add(SliderSubscribeEntryStream(redEntryStream, RedSlider));

            var greenEntryStream = GetEntryEventStream(GreenEntry);
            _subscriptions.Add(SliderSubscribeEntryStream(greenEntryStream, GreenSlider));

            var blueEntryStream = GetEntryEventStream(BlueEntry);
            _subscriptions.Add(SliderSubscribeEntryStream(blueEntryStream, BlueSlider));

            var alphaEntryStream = GetEntryEventStream(AlphaEntry);
            _subscriptions.Add(SliderSubscribeEntryStream(alphaEntryStream, AlphaSlider));

            var redValueStream = redSliderStream.Merge(redEntryStream);
            var greenValueStream = greenSliderStream.Merge(greenEntryStream);
            var blueValueStream = blueSliderStream.Merge(blueEntryStream);
            var alphaValueStream = alphaSliderStream.Merge(alphaEntryStream);

            var subscribe = alphaValueStream
                .CombineLatest(redValueStream, greenValueStream, blueValueStream, (alpha, red, green, blue) => Color.FromRgba(red, green, blue, alpha))
                .Do(color => Debug.WriteLine($"color={color}"))
                .Subscribe(color => Device.BeginInvokeOnMainThread(() => ColorCanvas.Color = color));
            _subscriptions.Add(subscribe);
        }

        private IDisposable SliderSubscribeEntryStream(IObservable<double> observable, Slider slider)
        {
            return observable.Delay(TimeSpan.FromMilliseconds(500))
                  .Subscribe(value => Device.BeginInvokeOnMainThread(() => { slider.Value = value; }));
        }

        private static Action<double> UpdateEntry(Entry entry)
        {
            return value =>
            {
                Device.BeginInvokeOnMainThread(() => entry.Text = ((int)Math.Round(255 * value)).ToString("X2"));
            };
        }

        private static IObservable<double> GetSliderEventStream(Slider slider, double initialValue)
        {
            return Observable.FromEventPattern<EventHandler<ValueChangedEventArgs>, ValueChangedEventArgs>(
                ev => slider.ValueChanged += ev,
                ev => slider.ValueChanged -= ev)
                .Select(x => x.EventArgs.NewValue)
                .Do(value => Debug.WriteLine($"slider newValue={value}"))
                .StartWith(initialValue);
        }

        private IObservable<double> GetEntryEventStream(Entry entry)
        {
            entry.Completed += (sender, args) => { };

            return Observable.FromEventPattern(
                    ev => entry.Completed += ev,
                    ev => entry.Completed -= ev)
                .Select(_ => entry.Text)
                .Do(value => Debug.WriteLine($"Entry Text={value}"))
                .Select(hexStr => Convert.ToDouble(ulong.Parse(hexStr, NumberStyles.AllowHexSpecifier)) / 255)
                .Do(value => Debug.WriteLine($"Entry double value={value}"));
        }
    }
}
