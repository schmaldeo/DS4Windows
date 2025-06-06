using DS4Windows;
using DS4Windows.InputDevices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WPFLocalizeExtension.Engine;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DS4WinWPF.DS4Forms.ViewModels
{
	internal class CustomDevicesEditorViewModel : INotifyPropertyChanged
	{
		private ObservableCollection<CustomDeviceVM> customDevicesVM;
		private CustomDeviceVM selectedDeviceVM;
		private string statusMessage = "";
		private bool isReadWriteLockActive = true;
		private bool isInEditMode;
		
		private bool isReadyToUse = false;
		private CustomDeviceVM editorVM;

		string PathToCustomDevsJson => DS4Devices.CustomDevicesJsonFilePath;

		/// <summary>
		/// View model for the custom devices types editor.
		/// </summary>
		public CustomDevicesEditorViewModel()
		{
			Initialization();
		}

		public ObservableCollection<CustomDeviceVM> CustomDevicesVM
		{
			get => customDevicesVM;
			set {
				customDevicesVM = value;
				OnPropertyChanged(nameof(CustomDevicesVM));
			}
		}

		public CustomDeviceVM SelectedDeviceVM
		{
			get => selectedDeviceVM;
			set {
				selectedDeviceVM = value;
				EditorVM = value == null ? null : new(value.GetDeviceInfo());
				OnPropertyChanged(nameof(SelectedDeviceVM));
			}
		}

		public CustomDeviceVM EditorVM
		{
			get => editorVM;
			private set {
				editorVM = value;
				OnPropertyChanged(nameof(EditorVM));
			}
		}

		public bool IsInEditMode
		{
			get => isInEditMode;
			set {
				isInEditMode = value;
				OnPropertyChanged(nameof(IsInEditMode));
			}
		}

		public bool IsReadyToUse
		{
			get => isReadyToUse;
			private set {

				isReadyToUse = value;
				OnPropertyChanged(nameof(IsReadyToUse));
			}
		}

		public string StatusMessage
		{
			get => statusMessage;
			private set {
				statusMessage = $"[{DateTime.Now.ToString("HH:mm:ss")}] {value}";
				OnPropertyChanged(nameof(StatusMessage));
			}
		}

		public bool IsReadWriteLockActive
		{
			get => isReadWriteLockActive;
			set {
				isReadWriteLockActive = value;
				OnPropertyChanged(nameof(IsReadWriteLockActive));
			}
		}

		public async Task<bool> Initialization()
		{
			if (await ReadCustomDevicesListFromDisk()) {
				IsReadyToUse = true;
				return true;
			}
			return false;
		}

		public async Task<bool> ReadCustomDevicesListFromDisk()
		{
			IsReadWriteLockActive = true;
			if (File.Exists(PathToCustomDevsJson)) {
				try {

					List<CustomDeviceInfo> customDeviceInfos = new List<CustomDeviceInfo>();

					var wot = await Task.Run(() => DS4Devices.LoadCustomDevicesListFromDisk());
					var wotwat = new ObservableCollection<CustomDeviceVM>();
					foreach (var devInfo in wot) {
						wotwat.Add(new(devInfo));
					}
					CustomDevicesVM = new ObservableCollection<CustomDeviceVM>(wotwat);
					IsReadWriteLockActive = false;
					StatusMessage = Translations.Strings.CustomDevices_Status_LoadSuccess;
					return true;
				}
				catch {
					StatusMessage = Translations.Strings.CustomDevices_StatusLoad_Failed;
				}
			}
			else {
				
				StatusMessage = Translations.Strings.CustomDevices_Status_ListNotFound;
			}

			return false;
		}

		public bool EditSelectedDevice()
		{
			if (EditorVM != null) {
				IsInEditMode = true;
				return true;
			}
			return false;
		}

		public async Task<bool> SaveCustomDevicesListToDisk()
		{
			bool status = true;
			IsReadWriteLockActive = true;
			StatusMessage = Translations.Strings.CustomDevices_Status_WritingToDisk;
			try {
				var listOfCustomDevs = CustomDevicesVM.ToList();
				await Task.Run(() => DS4Devices.SaveCustomDevicesListToDisk(GetCustomDevicesInfo()));
				StatusMessage = Translations.Strings.CustomDevices_Status_SaveSuccess;
			}
			catch {
				StatusMessage = Translations.Strings.CustomDevices_Status_SaveFailed;
				status = false;
			}
			IsReadWriteLockActive = false;
			return status;
		}

		public async Task<bool> SaveChanges()
		{
			IsReadWriteLockActive = true;
			if ( EditorVM == null) {
				return false;
			}

			CustomDeviceVM newVM = new(EditorVM.GetDeviceInfo());
			if(SelectedDeviceVM != null) {
				int index = CustomDevicesVM.IndexOf(SelectedDeviceVM);
				if (index >= 0) {
					CustomDevicesVM[index] = newVM;
				}
				else {
					return false;
				}
			}
			else{
				CustomDevicesVM.Add(newVM); 
			}
			SelectedDeviceVM = newVM;
			IsInEditMode = false;
			MakeChangesEffective();
			await SaveCustomDevicesListToDisk();
			return true;
		}

		private CustomDeviceInfo[] GetCustomDevicesInfo()
		{
			var customDevsInfo = new CustomDeviceInfo[CustomDevicesVM.Count];
			for ( int i = 0; i < CustomDevicesVM.Count; i++ ) {
				customDevsInfo[i] = CustomDevicesVM[i].GetDeviceInfo();
			}
			return customDevsInfo;
		}

		public bool DiscardChanges()
		{
			SelectedDeviceVM = null;
			IsInEditMode = false;
			return true;
		}

		public bool Add()
		{
			SelectedDeviceVM = null;
			var newCustomDev = new CustomDeviceInfo()
			{
				Name = "Custom Device Type Name",
				Vid = 0,
				Pid = 0,
				ConnectionTypeDeterminer = null,
				FeatureSet = VidPidFeatureSet.DefaultDS4,
			};
			EditorVM = new(newCustomDev);
			EditSelectedDevice();
			return true;
		}

		public async Task<bool> RemoveSelectedDevice() {
			if(SelectedDeviceVM == null) {
				return false;
			}
			
			int index = CustomDevicesVM.IndexOf(SelectedDeviceVM);
			if (index >= 0) {
				CustomDevicesVM.RemoveAt(index);
				await SaveCustomDevicesListToDisk();
				MakeChangesEffective();
			}
			SelectedDeviceVM = null;
			return true;;
	}

		public async void ResetCustomDevicesList()
		{
			IsReadWriteLockActive = false;
			IsInEditMode = false;
			SelectedDeviceVM = null;
			CustomDevicesVM = new();
			await SaveCustomDevicesListToDisk();
			IsReadyToUse = true;
			MakeChangesEffective();
		}

		public void MakeChangesEffective()
		{
			DS4Devices.SetCustomDevices(GetCustomDevicesInfo());
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	internal class CustomDeviceVM : INotifyPropertyChanged
	{
		public CustomDeviceVM(CustomDeviceInfo cDev)
		{
			EnableDetection = cDev.EnableDetection;
			Name = cDev.Name;
			Vid = cDev.Vid;
			Pid = cDev.Pid;
			var connectionTypeDeterminer = cDev.ConnectionTypeDeterminer;
			ConnectionType = connectionTypeDeterminer == null ? ConnectionTypeDeterminer.Undefined : cDev.ConnectionTypeDeterminer;
			InputDeviceType = cDev.InputDevType;
			Flags = (int)cDev.FeatureSet;
			FromFlags(cDev.FeatureSet);
		}

		public bool EnableDetection { get; set; }
		public string Name { get; set; }
		public int Vid { get; set; }
		public int Pid { get; set; }
		public string VidPidInHexString => $"{Vid:X4}/{Pid:X4}";

		public string FlagsAsBinaryString => Convert.ToString(Flags, 2).PadLeft(7, '0');
		public InputDeviceType InputDeviceType { get; set; }
		public ConnectionTypeDeterminer? ConnectionType { get; set; }

		public List<ConnectionTypeDeterminer> TypesOfConnectionDeterminers => new List<ConnectionTypeDeterminer>(){
			(int) ConnectionTypeDeterminer.Undefined,
			ConnectionTypeDeterminer.DS4,
			ConnectionTypeDeterminer.DualSense,
			ConnectionTypeDeterminer.SwitchPro,
			ConnectionTypeDeterminer.JoyCon,
			ConnectionTypeDeterminer.DS3,
				};

		public List<InputDeviceType> TypesOfInputDevice => new List<InputDeviceType>(){
			InputDeviceType.DS3,
			InputDeviceType.DS4,
			InputDeviceType.DualSense,
			InputDeviceType.SwitchPro,
			InputDeviceType.JoyConL,
			InputDeviceType.JoyConR,
			InputDeviceType.JoyConGrip,
				};

		public string ConnectionTypeAsString => ConnectionType?.ToString();
		public int Flags { get; }
		public bool IsDefaultDs4FlagEnabled { get; set; }
		public bool IsOnlyInputData0x01FlagEnabled { get; set; }
		public bool IsOnlyOutputData0x05FlagEnabled { get; set; }
		public bool IsNoOutputDataFlagEnabled { get; set; }
		public bool IsNoBatteryReadingFlagEnabled { get; set; }
		public bool IsNoGyroCalibtureSetFlagEnabled { get; set; }
		public bool IsMonitorAudioFlagEnabled { get; set; }
		public bool IsVendorDefinedDeviceFlagEnabled { get; set; }

		public bool IsInEditMode { get; set; }

		public bool ReadCustomDevicesListFromDisk()
		{
			return true;
		}

		public CustomDeviceInfo GetDeviceInfo()
		{
			return new CustomDeviceInfo()
			{
				EnableDetection = EnableDetection,
				Name = this.Name,
				Vid = this.Vid,
				Pid = this.Pid,
				InputDevType = this.InputDeviceType,
				ConnectionTypeDeterminer = this.ConnectionType,
				FeatureSet = (VidPidFeatureSet)ToUShortFlags()
			};
		}

		public ushort ToUShortFlags() =>
			(ushort)(
				(IsOnlyInputData0x01FlagEnabled ? 1 << 0 : 0) |
				(IsOnlyOutputData0x05FlagEnabled ? 1 << 1 : 0) |
				(IsNoOutputDataFlagEnabled ? 1 << 2 : 0) |
				(IsNoBatteryReadingFlagEnabled ? 1 << 3 : 0) |
				(IsNoGyroCalibtureSetFlagEnabled ? 1 << 4 : 0) |
				(IsMonitorAudioFlagEnabled ? 1 << 5 : 0) |
				(IsVendorDefinedDeviceFlagEnabled ? 1 << 6 : 0)
			);

		public void FromFlags(VidPidFeatureSet flags)
		{
			IsOnlyInputData0x01FlagEnabled = flags.HasFlag(VidPidFeatureSet.OnlyInputData0x01);
			IsOnlyOutputData0x05FlagEnabled = flags.HasFlag(VidPidFeatureSet.OnlyOutputData0x05);
			IsNoOutputDataFlagEnabled = flags.HasFlag(VidPidFeatureSet.NoOutputData);
			IsNoBatteryReadingFlagEnabled = flags.HasFlag(VidPidFeatureSet.NoBatteryReading);
			IsNoGyroCalibtureSetFlagEnabled = flags.HasFlag(VidPidFeatureSet.NoGyroCalib);
			IsMonitorAudioFlagEnabled = flags.HasFlag(VidPidFeatureSet.MonitorAudio);
			IsVendorDefinedDeviceFlagEnabled = flags.HasFlag(VidPidFeatureSet.VendorDefinedDevice);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
