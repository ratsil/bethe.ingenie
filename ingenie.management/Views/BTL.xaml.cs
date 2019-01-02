using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Collections.ObjectModel;
using ingenie.management.service;
using controls.childs.sl;

namespace ingenie.management.Views
{
    public partial class BTL : Page
    {
        private ManagementSoapClient _cIG;
		private EffectInfo[] _aEffectsInfo;
        private EffectInfo _cCurrentEffectInfo;
        private List<EffectInfo> _aSelectedItems;
        public BTL()
        {
            InitializeComponent();
			_cIG = new ManagementSoapClient();
			_aSelectedItems = new List<EffectInfo>();
            _cIG.BaetylusEffectsInfoGetCompleted += new EventHandler<BaetylusEffectsInfoGetCompletedEventArgs>(_cIG_BaetylusEffectsInfoGetCompleted);
            _cIG.BaetylusEffectStopCompleted += new EventHandler<BaetylusEffectStopCompletedEventArgs>(_cIG_BaetylusEffectStopCompleted);
			_cIG.RestartServicesCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(_cIG_RestartServicesCompleted);
            _cIG.BaetylusEffectsInfoGetAsync();
        }

        void _cIG_BaetylusEffectStopCompleted(object sender, BaetylusEffectStopCompletedEventArgs e)
        {
            string sErrorEIs = "";
            if (e.Result == null || e.UserState == null)
                MessageBox.Show("не удалось! [e.result=" + (e.Result == null ? "NULL" : "" + e.Result.Count) + "][e.result=" + (e.UserState == null ? "NULL" : "" + ((List<EffectInfo>)e.UserState).Count) + "]");
            foreach (EffectInfo cEI in (List<EffectInfo>)e.UserState)
            {
                if (cEI == null)
                {
                    sErrorEIs += Environment.NewLine + "вместо информации об эффекте получен NULL";
                    continue;
                }
                if (!e.Result.Contains(cEI.nHashCode))
                    cEI.sStatus = "stopped";
                else
                {
                    sErrorEIs += Environment.NewLine + cEI.nHashCode;
                }
            }
            if ("" != sErrorEIs)
                MessageBox.Show("Ошибка!", "Внимание, во время остановки этих элементов произошли ошибки: " + sErrorEIs + "\nсмотри лог!", MessageBoxButton.OK);
        }

        void _cIG_BaetylusEffectsInfoGetCompleted(object sender, BaetylusEffectsInfoGetCompletedEventArgs e)
        {
            if (null != e.Result)
            {
                _ui_dgBTLEffects.ItemsSource = null;
                _ui_dgBTLEffects.ItemsSource = _aEffectsInfo = e.Result;
            }
        }

		void _cIG_RestartServicesCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			string sMess;
			if (null == e.Error)
				sMess = "Команда на перезапуск была размещена успешно.\nСвязь с сервером будет потеряна, поэтому перезапустите браузер через 10 - 15 секунд.\n\nОб успешности перезапуска служб можно судить по логам.";
			else
				sMess = "Не удалось разместить команду на перезапуск! Нужно перезапускать в ручном режиме...\n\nтекст ошибки:";
			MsgBox msgResetOK = new MsgBox();
			msgResetOK.ShowError(sMess, e.Error);
		}









        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }
        private void _ui_dgBTLEffects_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _cCurrentEffectInfo = (EffectInfo)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
            }
            catch
            {
                _cCurrentEffectInfo = null;
            }
            _ui_cmBTLEffectsRefresh.IsEnabled = true;
            _ui_cmBTLEffectsStop.IsEnabled = true;
            VisualStateManager.GoToState(_ui_cmBTLEffectsStop, "Normal", true);
            _aSelectedItems.Clear();
            if (null == _cCurrentEffectInfo)
                _cCurrentEffectInfo = (EffectInfo)_ui_dgBTLEffects.SelectedItem;
            if (1 < _ui_dgBTLEffects.SelectedItems.Count)
            {
                foreach (EffectInfo cEI in _ui_dgBTLEffects.SelectedItems)
                    _aSelectedItems.Add(cEI);
                _ui_cmBTLEffectsStop.Header = "остановить: " + _aSelectedItems.Count + " items";
            }
            else if (null != _cCurrentEffectInfo)
            {
                _aSelectedItems.Add(_cCurrentEffectInfo);
                _ui_cmBTLEffectsStop.Header = "остановить: " + _cCurrentEffectInfo.sInfo;
            }
            else
            {
                _ui_cmBTLEffectsStop.IsEnabled = false;
                VisualStateManager.GoToState(_ui_cmBTLEffectsStop, "Disabled", true);
                _ui_cmBTLEffectsStop.Header = "остановить: не выбраны элементы";
            }
        }
        private void _ui_cmBTLEffects_Opened(object sender, RoutedEventArgs e)
        {
            //_ui_cmBTLEffectsRefresh.Focus();
            //_ui_cmBTLEffectsStop.Focus();
            //if (!_ui_cmBTLEffectsStop.IsEnabled)
            //    _ui_cmBTLEffectsStop.Foreground = new SolidColorBrush(Color.FromArgb(255, 140, 140, 140));
            //else
            //    _ui_cmBTLEffectsStop.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            //_ui_cmBTLEffects.Focus();
            //_ui_cmBTLEffectsStop.Visibility = System.Windows.Visibility.Collapsed;
            //_ui_cmBTLEffects.UpdateLayout();
            //_ui_cmBTLEffectsStop.UpdateLayout();
            //_ui_cmBTLEffectsStop.Visibility = System.Windows.Visibility.Visible;
            //_ui_cmBTLEffects.UpdateLayout();
            //_ui_cmBTLEffectsStop.UpdateLayout();
        }

        private void _ui_cmBTLEffectsRefresh_Click(object sender, RoutedEventArgs e)
        {
            _cIG.BaetylusEffectsInfoGetAsync();
        }

        private void _ui_cmBTLEffectsStop_Click(object sender, RoutedEventArgs e)
        {
            _cIG.BaetylusEffectStopAsync(_aSelectedItems.ToArray(), _aSelectedItems);
        }

        private void _ui_dgBTLEffects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

		private void _ui_btnSCRReset_Click(object sender, RoutedEventArgs e)
		{
			MsgBox msgReset = new MsgBox("Будут перезапущены службы IIS, а также сервисы IG, что приведет к \n!!!ПОЛНОМУ ПРЕКРАЩЕНИЮ РАБОТЫ ПРЯМОГО ЭФИРА!!!\n\n??????УВЕРЕНЫ??????", "ВНИМАНИЕ!", MsgBox.MsgBoxButton.OKCancel);
			msgReset.Closed += new EventHandler(msgReset_Closed);
			msgReset.Show();
		}

		void msgReset_Closed(object sender, EventArgs e)
		{
			if (((MsgBox)sender).enMsgResult == MsgBox.MsgBoxButton.OK)
				_cIG.RestartServicesAsync();
		}

        

    }
}
