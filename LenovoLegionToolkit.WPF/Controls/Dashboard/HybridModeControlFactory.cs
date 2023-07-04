﻿using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Features.Hybrid;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;
using LenovoLegionToolkit.WPF.Windows.Dashboard;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

namespace LenovoLegionToolkit.WPF.Controls.Dashboard;

public static class HybridModeControlFactory
{
    public static async Task<AbstractRefreshingControl> GetControlAsync()
    {
        var mi = await Compatibility.GetMachineInformationAsync();
        if (mi.Properties.SupportsIGPUMode)
            return new ComboBoxHybridModeControl();
        return new ToggleHybridModeControl();
    }

    private class ComboBoxHybridModeControl : AbstractComboBoxFeatureCardControl<HybridModeState>
    {
        private readonly Button _infoButton = new()
        {
            Icon = SymbolRegular.Info24,
            FontSize = 20,
            Margin = new(8, 0, 0, 0),
        };

        public ComboBoxHybridModeControl()
        {
            Icon = SymbolRegular.LeafOne24;
            Title = Resource.ComboBoxHybridModeControl_Title;
            Subtitle = Resource.ComboBoxHybridModeControl_Message;
        }

        protected override FrameworkElement GetAccessory(ComboBox comboBox)
        {
            comboBox.MinWidth = 150;

            _infoButton.Click += InfoButton_Click;

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
            };
            stackPanel.Children.Add(comboBox);
            stackPanel.Children.Add(_infoButton);

            return stackPanel;
        }

        protected override async Task OnStateChange(ComboBox comboBox, IFeature<HybridModeState> feature, HybridModeState? newValue, HybridModeState? oldValue)
        {
            if (newValue is null || oldValue is null)
                return;

            await base.OnStateChange(comboBox, feature, newValue, oldValue);

            if (newValue != HybridModeState.Off && oldValue != HybridModeState.Off)
            {
                await RefreshAsync();
                return;
            }

            var result = await MessageBoxHelper.ShowAsync(
                this,
                Resource.ComboBoxHybridModeControl_RestartRequired_Title,
                string.Format(Resource.ComboBoxHybridModeControl_RestartRequired_Message, newValue.GetDisplayName()),
                Resource.RestartNow,
                Resource.RestartLater);

            if (result)
                await Power.RestartAsync();
            else
                await RefreshAsync();
        }

        protected override void OnStateChangeException(Exception exception)
        {
            if (exception is IGPUModeChangeException { IGPUMode: not IGPUModeState.Default } ex1)
            {
                var (title, message) = ex1.IGPUMode switch
                {
                    IGPUModeState.IGPUOnly => (Resource.IGPUModeChangeException_Title_IGPUOnly, Resource.IGPUModeChangeException_Message_IGPUOnly),
                    IGPUModeState.Auto => (Resource.IGPUModeChangeException_Title_Auto, Resource.IGPUModeChangeException_Message_Auto),
                    _ => (Resource.IGPUModeChangeException_Title, Resource.IGPUModeChangeException_Message)
                };

                SnackbarHelper.Show(title, message, SnackbarType.Warning);
            }
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ExtendedHybridModeInfoWindow { Owner = Window.GetWindow(this) };
            window.ShowDialog();
        }
    }

    private class ToggleHybridModeControl : AbstractToggleFeatureCardControl<HybridModeState>
    {
        protected override HybridModeState OnState => HybridModeState.On;

        protected override HybridModeState OffState => HybridModeState.Off;

        public ToggleHybridModeControl()
        {
            Icon = SymbolRegular.LeafOne24;
            Title = Resource.ToggleHybridModeControl_Title;
            Subtitle = Resource.ToggleHybridModeControl_Message;
        }

        protected override async Task OnStateChange(ToggleSwitch toggle, IFeature<HybridModeState> feature)
        {
            await base.OnStateChange(toggle, feature);

            var result = await MessageBoxHelper.ShowAsync(
                this,
                Resource.ToggleHybridModeControl_RestartRequired_Title,
                Resource.ToggleHybridModeControl_RestartRequired_Message,
                Resource.RestartNow,
                Resource.RestartLater);

            if (result)
                await Power.RestartAsync();
        }
    }
}
