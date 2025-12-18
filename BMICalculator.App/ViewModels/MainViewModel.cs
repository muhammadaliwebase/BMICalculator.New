using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BMICalculator.Agent.Configuration;
using BMICalculator.Agent.Models;
using BMICalculator.Agent.Services;
using BMICalculator.Core.Configuration;
using BMICalculator.Core.Models;
using BMICalculator.Core.Services;

namespace BMICalculator.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private SerialPort? _serialPort;
    private CancellationTokenSource? _cancellationTokenSource;
    private List<double> _weightReadings = new List<double>();
    private List<double> _heightReadings = new List<double>();
    private bool _isCollectingData = false;

    // Services
    private readonly IFaceIdListenerService _faceIdListener;
    private readonly IWbAccessControlApiClient _apiClient;

    #region Current Measurement Properties

    private double _weight;
    public double Weight
    {
        get => _weight;
        set
        {
            _weight = value;
            OnPropertyChanged(nameof(Weight));
            CalculateBMI();
            UpdateBmiChange();
        }
    }

    private double _height;
    public double Height
    {
        get => _height;
        set
        {
            _height = value;
            OnPropertyChanged(nameof(Height));
            CalculateBMI();
        }
    }

    private double _bmi;
    public double BMI
    {
        get => _bmi;
        set
        {
            _bmi = value;
            OnPropertyChanged(nameof(BMI));
            UpdateBMICategory();
            UpdateBmiChange();
        }
    }

    private string _bmiCategory = "Normal";
    public string BMICategory
    {
        get => _bmiCategory;
        set
        {
            _bmiCategory = value;
            OnPropertyChanged(nameof(BMICategory));
        }
    }

    private Brush _categoryColor = new SolidColorBrush(Color.FromRgb(102, 187, 106));
    public Brush CategoryColor
    {
        get => _categoryColor;
        set
        {
            _categoryColor = value;
            OnPropertyChanged(nameof(CategoryColor));
        }
    }

    private double _needleAngle = -90;
    public double NeedleAngle
    {
        get => _needleAngle;
        set
        {
            _needleAngle = value;
            OnPropertyChanged(nameof(NeedleAngle));
        }
    }

    #endregion

    #region Person Properties (from FaceID)

    private string? _currentPersonId;
    public string? CurrentPersonId
    {
        get => _currentPersonId;
        set
        {
            _currentPersonId = value;
            OnPropertyChanged(nameof(CurrentPersonId));
            OnPropertyChanged(nameof(HasCurrentPerson));
            OnPropertyChanged(nameof(CanSaveMeasurement));
        }
    }

    private string _currentPersonName = "Kutilmoqda...";
    public string CurrentPersonName
    {
        get => _currentPersonName;
        set
        {
            _currentPersonName = value;
            OnPropertyChanged(nameof(CurrentPersonName));
        }
    }

    private string? _currentPersonPosition;
    public string? CurrentPersonPosition
    {
        get => _currentPersonPosition;
        set
        {
            _currentPersonPosition = value;
            OnPropertyChanged(nameof(CurrentPersonPosition));
        }
    }

    public bool HasCurrentPerson => !string.IsNullOrEmpty(CurrentPersonId);

    #endregion

    #region Old BMI Properties (from API)

    private double? _oldBmi;
    public double? OldBmi
    {
        get => _oldBmi;
        set
        {
            _oldBmi = value;
            OnPropertyChanged(nameof(OldBmi));
            OnPropertyChanged(nameof(HasOldBmi));
            UpdateBmiChange();
        }
    }

    private double? _oldWeight;
    public double? OldWeight
    {
        get => _oldWeight;
        set
        {
            _oldWeight = value;
            OnPropertyChanged(nameof(OldWeight));
        }
    }

    private double? _oldHeight;
    public double? OldHeight
    {
        get => _oldHeight;
        set
        {
            _oldHeight = value;
            OnPropertyChanged(nameof(OldHeight));
        }
    }

    private string? _oldBmiCategory;
    public string? OldBmiCategory
    {
        get => _oldBmiCategory;
        set
        {
            _oldBmiCategory = value;
            OnPropertyChanged(nameof(OldBmiCategory));
        }
    }

    private DateTime? _oldMeasuredAt;
    public DateTime? OldMeasuredAt
    {
        get => _oldMeasuredAt;
        set
        {
            _oldMeasuredAt = value;
            OnPropertyChanged(nameof(OldMeasuredAt));
        }
    }

    public bool HasOldBmi => OldBmi.HasValue;

    private double? _bmiChange;
    public double? BmiChange
    {
        get => _bmiChange;
        set
        {
            _bmiChange = value;
            OnPropertyChanged(nameof(BmiChange));
            OnPropertyChanged(nameof(BmiChangeText));
            OnPropertyChanged(nameof(BmiChangeColor));
        }
    }

    public string BmiChangeText
    {
        get
        {
            if (!BmiChange.HasValue) return "";
            var change = BmiChange.Value;
            var sign = change >= 0 ? "+" : "";
            return $"{sign}{change:F2}";
        }
    }

    public Brush BmiChangeColor
    {
        get
        {
            if (!BmiChange.HasValue) return Brushes.Gray;
            // For BMI, decrease is generally better (if overweight)
            // But we'll show green for decrease, red for increase
            return BmiChange.Value <= 0
                ? new SolidColorBrush(Color.FromRgb(102, 187, 106)) // Green
                : new SolidColorBrush(Color.FromRgb(239, 83, 80));  // Red
        }
    }

    #endregion

    #region Serial Port Properties

    private ObservableCollection<string> _availablePorts = new ObservableCollection<string>();
    public ObservableCollection<string> AvailablePorts
    {
        get => _availablePorts;
        set
        {
            _availablePorts = value;
            OnPropertyChanged(nameof(AvailablePorts));
        }
    }

    private string? _selectedPort;
    public string? SelectedPort
    {
        get => _selectedPort;
        set
        {
            _selectedPort = value;
            OnPropertyChanged(nameof(SelectedPort));
        }
    }

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            _isConnected = value;
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(ConnectionButtonText));
        }
    }

    public string ConnectionButtonText => IsConnected ? "Disconnect" : "Connect";

    #endregion

    #region FaceID Listener Properties

    private bool _isFaceIdListening;
    public bool IsFaceIdListening
    {
        get => _isFaceIdListening;
        set
        {
            _isFaceIdListening = value;
            OnPropertyChanged(nameof(IsFaceIdListening));
            OnPropertyChanged(nameof(FaceIdButtonText));
        }
    }

    public string FaceIdButtonText => IsFaceIdListening ? "Stop FaceID" : "Start FaceID";

    private string _faceIdStatus = "FaceID: Not listening";
    public string FaceIdStatus
    {
        get => _faceIdStatus;
        set
        {
            _faceIdStatus = value;
            OnPropertyChanged(nameof(FaceIdStatus));
        }
    }

    #endregion

    #region Status Properties

    private string _statusMessage = "Disconnected";
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    private string _processingMessage = "Waiting for data...";
    public string ProcessingMessage
    {
        get => _processingMessage;
        set
        {
            _processingMessage = value;
            OnPropertyChanged(nameof(ProcessingMessage));
        }
    }

    private bool _isProcessing;
    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            _isProcessing = value;
            OnPropertyChanged(nameof(IsProcessing));
        }
    }

    private bool _isSaving;
    public bool IsSaving
    {
        get => _isSaving;
        set
        {
            _isSaving = value;
            OnPropertyChanged(nameof(IsSaving));
            OnPropertyChanged(nameof(CanSaveMeasurement));
        }
    }

    public bool CanSaveMeasurement => HasCurrentPerson && BMI > 0 && !IsSaving;

    #endregion

    #region Commands

    public ICommand ToggleConnectionCommand { get; }
    public ICommand ToggleFaceIdCommand { get; }
    public ICommand SaveMeasurementCommand { get; }
    public ICommand ClearPersonCommand { get; }

    #endregion

    public MainViewModel()
    {
        // Initialize services
        var faceIdConfig = new FaceIdListenerConfig { ListenPort = 8080 };
        _faceIdListener = new FaceIdListenerService(faceIdConfig);
        _faceIdListener.PersonScanned += OnPersonScanned;

        var apiConfig = new ApiConfiguration
        {
            BaseUrl = "http://wbac-api.apptest.uz",
            Username = "admin",
            Password = "123456"
        };
        _apiClient = new WbAccessControlApiClient(new HttpClient(), apiConfig);

        // Initialize commands
        ToggleConnectionCommand = new RelayCommand(async _ => await ToggleConnection());
        ToggleFaceIdCommand = new RelayCommand(async _ => await ToggleFaceId());
        SaveMeasurementCommand = new RelayCommand(async _ => await SaveMeasurement(), _ => CanSaveMeasurement);
        ClearPersonCommand = new RelayCommand(_ => ClearCurrentPerson());

        RefreshAvailablePorts();

        // Set default values
        Weight = 0;
        Height = 0;

        // Auto-start FaceID listener
        _ = ToggleFaceId();
    }

    #region FaceID Methods

    private async Task ToggleFaceId()
    {
        try
        {
            if (IsFaceIdListening)
            {
                await _faceIdListener.StopAsync();
                IsFaceIdListening = false;
                FaceIdStatus = "FaceID: Stopped";
            }
            else
            {
                await _faceIdListener.StartAsync();
                IsFaceIdListening = true;
                FaceIdStatus = $"FaceID: Listening on {_faceIdListener.ListenUrl}";

                // Authenticate with API
                var authenticated = await _apiClient.AuthenticateAsync("Agent001", "123456");
                if (!authenticated)
                {
                    FaceIdStatus += " (API auth failed)";
                }
            }
        }
        catch (Exception ex)
        {
            FaceIdStatus = $"FaceID Error: {ex.Message}";
        }
    }

    private async void OnPersonScanned(object? sender, FaceIdScanEvent e)
    {
        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                CurrentPersonId = e.PersonId;
                CurrentPersonName = e.Name ?? "Loading...";
                FaceIdStatus = $"FaceID: Scanned {e.PersonId}";

                // Fetch person details
                var person = await _apiClient.GetPersonByIdAsync(e.PersonId);
                if (person != null)
                {
                    CurrentPersonName = person.FullName;
                    CurrentPersonPosition = person.Position;
                }

                // Fetch old BMI data
                var oldBmi = await _apiClient.GetLatestBmiByPersonIdAsync(e.PersonId);
                if (oldBmi != null)
                {
                    OldBmi = (double)oldBmi.Bmi;
                    OldWeight = (double)oldBmi.Weight;
                    OldHeight = (double)oldBmi.Height;
                    OldBmiCategory = oldBmi.BmiCategory;
                    OldMeasuredAt = oldBmi.MeasuredAt;
                }
                else
                {
                    ClearOldBmiData();
                }

                ProcessingMessage = $"Person ready: {CurrentPersonName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading person: {ex.Message}";
            }
        });
    }

    private void ClearCurrentPerson()
    {
        CurrentPersonId = null;
        CurrentPersonName = "Kutilmoqda...";
        CurrentPersonPosition = null;
        ClearOldBmiData();
        ProcessingMessage = "Waiting for FaceID scan...";
    }

    private void ClearOldBmiData()
    {
        OldBmi = null;
        OldWeight = null;
        OldHeight = null;
        OldBmiCategory = null;
        OldMeasuredAt = null;
        BmiChange = null;
    }

    #endregion

    #region Save Measurement

    private async Task SaveMeasurement()
    {
        if (!CanSaveMeasurement) return;

        try
        {
            IsSaving = true;
            StatusMessage = "Saving measurement...";

            var dto = new CreateBmiMeasurementDto
            {
                TurnstilePersonId = CurrentPersonId!,
                Weight = (decimal)Weight,
                Height = (decimal)Height,
                Bmi = (decimal)BMI,
                BmiCategory = BMICategory,
                MeasuredAt = DateTime.Now,
                DeviceId = "BMICalculator"
            };

            var id = await _apiClient.CreateBmiMeasurementAsync(dto);
            if (id.HasValue)
            {
                StatusMessage = "Measurement saved successfully!";
                ProcessingMessage = $"Saved for {CurrentPersonName} (ID: {id})";

                // Update old BMI with current values for next comparison
                OldBmi = BMI;
                OldWeight = Weight;
                OldHeight = Height;
                OldBmiCategory = BMICategory;
                OldMeasuredAt = DateTime.Now;

                // Clear current person after successful save
                await Task.Delay(2000);
                ClearCurrentPerson();
            }
            else
            {
                StatusMessage = "Failed to save measurement";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save error: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    #endregion

    #region Serial Port Methods

    private void RefreshAvailablePorts()
    {
        AvailablePorts.Clear();
        var ports = SerialPort.GetPortNames();

        if (ports.Length == 0)
        {
            AvailablePorts.Add("No ports available");
        }
        else
        {
            foreach (var port in ports)
            {
                AvailablePorts.Add(port);
            }
            SelectedPort = ports[0];
        }
    }

    private async Task ToggleConnection()
    {
        if (IsConnected)
        {
            await DisconnectSerial();
        }
        else
        {
            await ConnectSerial();
        }
    }

    private async Task ConnectSerial()
    {
        try
        {
            if (string.IsNullOrEmpty(SelectedPort) || SelectedPort == "No ports available")
            {
                MessageBox.Show("Please select a valid COM port", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _serialPort = new SerialPort(SelectedPort, 9600)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };

            _serialPort.Open();
            IsConnected = true;
            StatusMessage = $"Connected to {SelectedPort}";

            _cancellationTokenSource = new CancellationTokenSource();
            await Task.Run(() => ReadSerialData(_cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Connection error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = $"Connection failed: {ex.Message}";
        }
    }

    private async Task DisconnectSerial()
    {
        try
        {
            _cancellationTokenSource?.Cancel();

            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
                _serialPort.Dispose();
            }

            IsConnected = false;
            StatusMessage = "Disconnected";
            ProcessingMessage = "Waiting for data...";
            IsProcessing = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Disconnection error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ReadSerialData(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _serialPort != null && _serialPort.IsOpen)
        {
            try
            {
                string line = _serialPort.ReadLine().Trim();

                if (!string.IsNullOrEmpty(line))
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ProcessSerialData(line);
                    });
                }
            }
            catch (TimeoutException)
            {
                continue;
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"Read error: {ex.Message}";
                });

                if (_serialPort == null || !_serialPort.IsOpen)
                    break;
            }

            await Task.Delay(10, cancellationToken);
        }
    }

    private void ProcessSerialData(string data)
    {
        try
        {
            var realTimeMatch = Regex.Match(data, @"\{real_time:\s*weight;\s*(\d+(?:\.\d+)?),\s*height;\s*(\d+(?:\.\d+)?)\}");
            if (realTimeMatch.Success)
            {
                double weight = double.Parse(realTimeMatch.Groups[1].Value);
                double height = double.Parse(realTimeMatch.Groups[2].Value);

                Weight = weight;
                Height = height;

                if (!_isCollectingData)
                {
                    ProcessingMessage = $"Real-time - Weight: {weight:F1} Kg, Height: {height:F0} cm";
                    StatusMessage = "Reading real-time data...";
                }
                return;
            }

            var buttonMatch = Regex.Match(data, @"\{click_button:\s*true\}");
            if (buttonMatch.Success)
            {
                StartDataCollection();
                return;
            }

            var dataMatch = Regex.Match(data, @"\{weight:\s*(\d+(?:\.\d+)?),\s*height:\s*(\d+(?:\.\d+)?)\}");
            if (dataMatch.Success && _isCollectingData)
            {
                double weight = double.Parse(dataMatch.Groups[1].Value);
                double height = double.Parse(dataMatch.Groups[2].Value);

                _weightReadings.Add(weight);
                _heightReadings.Add(height);

                ProcessingMessage = $"Collecting data: {_weightReadings.Count}/20 readings";

                if (_weightReadings.Count >= 20)
                {
                    FinishDataCollection();
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Parse error: {ex.Message}";
        }
    }

    private void StartDataCollection()
    {
        _isCollectingData = true;
        _weightReadings.Clear();
        _heightReadings.Clear();

        IsProcessing = true;
        ProcessingMessage = "Button pressed! Collecting 20 readings...";
        StatusMessage = "Collecting measurement data...";

        Weight = 0;
        Height = 0;
        BMI = 0;
    }

    private void FinishDataCollection()
    {
        _isCollectingData = false;
        IsProcessing = false;

        double avgWeight = _weightReadings.Average();
        double avgHeight = _heightReadings.Average();

        Weight = Math.Round(avgWeight, 1);
        Height = Math.Round(avgHeight, 0);

        ProcessingMessage = $"Measurement complete! Weight: {Weight:F1} Kg, Height: {Height:F0} cm";
        StatusMessage = $"Data collected ({_weightReadings.Count} readings)";

        CalculateBMI();
    }

    #endregion

    #region BMI Calculation

    private void CalculateBMI()
    {
        if (Height > 0 && Weight > 0)
        {
            double heightInMeters = Height / 100.0;
            BMI = Weight / (heightInMeters * heightInMeters);
        }
        else
        {
            BMI = 0;
        }
    }

    private void UpdateBMICategory()
    {
        if (BMI < 18.5)
        {
            BMICategory = "Underweight";
            CategoryColor = new SolidColorBrush(Color.FromRgb(253, 216, 53));
            NeedleAngle = -90 + (BMI / 18.5) * 45;
        }
        else if (BMI < 25)
        {
            BMICategory = "Normal";
            CategoryColor = new SolidColorBrush(Color.FromRgb(102, 187, 106));
            NeedleAngle = -45 + ((BMI - 18.5) / 6.5) * 45;
        }
        else if (BMI < 30)
        {
            BMICategory = "Overweight";
            CategoryColor = new SolidColorBrush(Color.FromRgb(255, 167, 38));
            NeedleAngle = 0 + ((BMI - 25) / 5) * 45;
        }
        else
        {
            BMICategory = "Obese";
            CategoryColor = new SolidColorBrush(Color.FromRgb(239, 83, 80));
            double angle = 45 + ((BMI - 30) / 10) * 45;
            NeedleAngle = Math.Min(angle, 90);
        }
    }

    private void UpdateBmiChange()
    {
        if (OldBmi.HasValue && BMI > 0)
        {
            BmiChange = BMI - OldBmi.Value;
        }
        else
        {
            BmiChange = null;
        }
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _faceIdListener.PersonScanned -= OnPersonScanned;
        (_faceIdListener as IDisposable)?.Dispose();
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _serialPort?.Dispose();
    }

    #endregion
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);

    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
