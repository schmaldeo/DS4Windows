using DS4WinWPF.DS4Forms.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DS4WinWPF.DS4Forms
{
	/// <summary>
	/// Interaction logic for CustomDevicesEditor.xaml
	/// </summary>
	public partial class CustomDevicesEditor : UserControl
	{
		CustomDevicesEditorViewModel ViewModel;

		public CustomDevicesEditor()
		{
			ViewModel = new CustomDevicesEditorViewModel();
			DataContext = ViewModel;
			InitializeComponent();
			ResertInfoDescriptionTextToDefault();
		}

		private void AddBtn_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.Add();
        }

		private void EditBtn_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.EditSelectedDevice();
		}

		private void RemoveBtn_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.RemoveSelectedDevice();
		}

		private void SaveBtn_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.SaveChanges();
		}

		private void CancelChangesBtn_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.DiscardChanges();
		}

		private void OptionsBtn_Click(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button?.ContextMenu != null) {
				// Position the context menu at the bottom-left of the button
				button.ContextMenu.PlacementTarget = button;
				button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
				button.ContextMenu.IsOpen = true;
			}
		}

		private void ResetListMenuItemBtn_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.ResetCustomDevicesList();
		}

		private void Flag0_ChkBox_MouseEnter(object sender, MouseEventArgs e)
		{
			var nD = DS4WinWPF.Translations.Strings.CustomDevices_Flag0_Info;
			UpdateInfoDescriptionText(nD);
        }

		private void Flag1_ChkBox_MouseEnter(object sender, MouseEventArgs e)
		{
			var nD = DS4WinWPF.Translations.Strings.CustomDevices_Flag1_Info;
			UpdateInfoDescriptionText(nD);
		}

		private void Flag2_ChkBox_MouseEnter(object sender, MouseEventArgs e)
		{
			var nD = DS4WinWPF.Translations.Strings.CustomDevices_Flag2_Info;
			UpdateInfoDescriptionText(nD);
		}

		private void Flag3_ChkBox_MouseEnter(object sender, MouseEventArgs e)
		{
			var nD = DS4WinWPF.Translations.Strings.CustomDevices_Flag3_Info;
			UpdateInfoDescriptionText(nD);
		}

		private void Flag4_ChkBox_MouseEnter(object sender, MouseEventArgs e)
		{
			var nD = DS4WinWPF.Translations.Strings.CustomDevices_Flag4_Info;
			UpdateInfoDescriptionText(nD);
		}

		private void Flag5_ChkBox_MouseEnter(object sender, MouseEventArgs e)
		{
			var nD = DS4WinWPF.Translations.Strings.CustomDevices_Flag5_Info;
			UpdateInfoDescriptionText(nD);
		}

		private void Flag6_ChkBox_MouseEnter(object sender, MouseEventArgs e)
		{
			var nD = DS4WinWPF.Translations.Strings.CustomDevices_Flag6_Info;
			UpdateInfoDescriptionText(nD);
		}

		private void EnableDetection_ChxBox_MouseEnter(object sender, MouseEventArgs e)
		{
			var nD = DS4WinWPF.Translations.Strings.CustomDevices_EnableDetectionDescription;
			UpdateInfoDescriptionText(nD);
		}

		private void HexTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9A-Fa-f]+$");
		}

		private void HexTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
		{
			if (e.DataObject.GetDataPresent(typeof(string))) {
				var text = (string)e.DataObject.GetData(typeof(string));
				if (!System.Text.RegularExpressions.Regex.IsMatch(text, "^[0-9A-Fa-f]+$")) {
					e.CancelCommand();
				}
			}
			else {
				e.CancelCommand();
			}
		}

		private void SaveBtn_MouseEnter(object sender, MouseEventArgs e)
		{
			var nD = DS4WinWPF.Translations.Strings.CustomDevices_SaveChangesBtn_Info;
			UpdateInfoDescriptionText(nD);
		}

		private void UpdateInfoDescriptionText(string description)
		{
			InfoDescription_TextBox.Text = description;
		}

		private void Name_TextBox_MouseEnter(object sender, MouseEventArgs e)
		{
			var nD = DS4WinWPF.Translations.Strings.CustomDevices_NameTextbox_Info;
			UpdateInfoDescriptionText(nD);
		}

		private void Vid_TextBox_MouseEnter(object sender, MouseEventArgs e)
		{
			ShowVidPidInformationDescription();
		}

		private void ShowVidPidInformationDescription()
		{
			var nD = DS4WinWPF.Translations.Strings.CustomDevices_VidPid_Info;
			UpdateInfoDescriptionText(nD);
		}

		private void Pid_TextBox_MouseEnter(object sender, MouseEventArgs e)
		{
			ShowVidPidInformationDescription();
		}

		private void InputDevType_Combobox_MouseEnter(object sender, MouseEventArgs e)
		{
			var nD = DS4WinWPF.Translations.Strings.CustomDevices_InputDeviceType_Info;
			UpdateInfoDescriptionText(nD);
		}

		private void ConTypeDeterminer_ComboBox_MouseEnter(object sender, MouseEventArgs e)
		{
			var nD = DS4WinWPF.Translations.Strings.CustomDevices_ConnectionType_Description;
			UpdateInfoDescriptionText(nD);
		}

		private void EditorArea_Border_MouseLeave(object sender, MouseEventArgs e)
		{
			ResertInfoDescriptionTextToDefault();
		}

		private void ResertInfoDescriptionTextToDefault()
		{
			var nT = "";
			UpdateInfoDescriptionText(nT);
			
		}
	}


}
