using System.Collections.Generic;
using System.Linq;
using System.Windows;
using XiloAdventures.Engine.Models;

namespace XiloAdventures.Wpf.Windows;

public partial class AddDoorWindow : Window
{
    public string? SelectedRoomBId { get; private set; }

    private readonly Room _roomA;
    private readonly IReadOnlyList<Room> _candidateRooms;

    public AddDoorWindow(Room roomA, IReadOnlyList<Room> candidateRooms)
    {
        InitializeComponent();

        _roomA = roomA;
        _candidateRooms = candidateRooms;

        RoomALabel.Text = string.IsNullOrWhiteSpace(_roomA.Name)
            ? _roomA.Id
            : _roomA.Name;

        RoomBCombo.ItemsSource = _candidateRooms;

        if (_candidateRooms.Count > 0)
        {
            RoomBCombo.SelectedIndex = 0;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (RoomBCombo.SelectedItem is Room roomB)
        {
            SelectedRoomBId = roomB.Id;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show(this,
                "Selecciona una sala destino.",
                "Crear puerta",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
