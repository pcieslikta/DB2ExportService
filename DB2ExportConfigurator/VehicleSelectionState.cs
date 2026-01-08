using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DB2ExportService.Models;

namespace DB2ExportConfigurator
{
    /// <summary>
    /// Represents the selection status of a vehicle
    /// </summary>
    public enum SelectionStatus
    {
        ConfigMatch,      // W config i zaznaczone ✓
        ConfigMismatch,   // W config ale NIE zaznaczone ⚠️
        ManualAdd,        // NIE w config ale zaznaczone ℹ️
        Unselected,       // Niezaznaczone
        NotInDatabase     // W config ale nie w DB ❌
    }

    /// <summary>
    /// Represents the state of a single vehicle in the grid
    /// </summary>
    public class VehicleSelectionState
    {
        public VehicleInfo Vehicle { get; set; } = null!;
        public bool IsInConfig { get; set; }
        public bool IsInDatabase { get; set; }
        public bool IsSelected { get; set; }
        public bool HasGates { get; set; }

        /// <summary>
        /// Gets the selection status based on current state
        /// </summary>
        public SelectionStatus Status
        {
            get
            {
                if (!IsInDatabase && IsInConfig)
                    return SelectionStatus.NotInDatabase;

                if (IsInConfig && IsSelected)
                    return SelectionStatus.ConfigMatch;

                if (IsInConfig && !IsSelected)
                    return SelectionStatus.ConfigMismatch;

                if (!IsInConfig && IsSelected)
                    return SelectionStatus.ManualAdd;

                return SelectionStatus.Unselected;
            }
        }

        /// <summary>
        /// Gets the row background color based on selection status
        /// </summary>
        public Color GetRowBackColor(bool isDarkMode)
        {
            return Status switch
            {
                // ConfigMatch: W config i zaznaczone ✓ - CIEMNOZIELONY
                SelectionStatus.ConfigMatch => isDarkMode
                    ? Color.FromArgb(30, 80, 40)      // Dark green (dark mode)
                    : Color.FromArgb(200, 230, 200),  // Light green (light mode)

                // ConfigMismatch: W config ale NIE zaznaczone ⚠️ - CZERWONY/RÓŻOWY
                SelectionStatus.ConfigMismatch => isDarkMode
                    ? Color.FromArgb(80, 30, 30)      // Dark red (dark mode)
                    : Color.FromArgb(255, 200, 200),  // Light red/pink (light mode)

                // ManualAdd: NIE w config ale zaznaczone ℹ️ - NIEBIESKI
                SelectionStatus.ManualAdd => isDarkMode
                    ? Color.FromArgb(30, 50, 80)      // Dark blue (dark mode)
                    : Color.FromArgb(220, 235, 255),  // Light blue (light mode)

                // NotInDatabase: W config ale nie w DB ❌ - POMARAŃCZOWY
                SelectionStatus.NotInDatabase => isDarkMode
                    ? Color.FromArgb(80, 60, 20)      // Dark orange (dark mode)
                    : Color.FromArgb(255, 230, 180),  // Light orange (light mode)

                // Unselected: Niezaznaczone - DOMYŚLNE TŁO
                _ => isDarkMode
                    ? Color.FromArgb(45, 45, 48)      // Dark background
                    : Color.White                     // Light background
            };
        }
    }

    /// <summary>
    /// Manages synchronization between config, database, and user selection
    /// </summary>
    public class VehicleSelectionSynchronizer
    {
        public List<int> ConfigNumbers { get; set; } = new();
        public HashSet<int> SelectedNBs { get; set; } = new();
        public List<VehicleInfo> DatabaseVehicles { get; set; } = new();

        /// <summary>
        /// Gets all vehicle selection states
        /// </summary>
        public List<VehicleSelectionState> GetStates()
        {
            return DatabaseVehicles.Select(v => new VehicleSelectionState
            {
                Vehicle = v,
                IsInConfig = ConfigNumbers.Contains(v.NB),
                IsInDatabase = true,
                IsSelected = SelectedNBs.Contains(v.NB),
                HasGates = v.MaBramki == "Y" || v.MaBramki == "1"
            }).ToList();
        }

        /// <summary>
        /// Gets the changes between config and current selection
        /// Returns (added, removed) vehicle numbers
        /// </summary>
        public (List<int> added, List<int> removed) GetChanges()
        {
            var added = SelectedNBs.Except(ConfigNumbers).OrderBy(nb => nb).ToList();
            var removed = ConfigNumbers.Except(SelectedNBs).OrderBy(nb => nb).ToList();
            return (added, removed);
        }

        /// <summary>
        /// Gets config numbers that were not found in the database
        /// </summary>
        public List<int> GetNotFoundInDatabase()
        {
            var dbNBs = DatabaseVehicles.Select(v => v.NB).ToHashSet();
            return ConfigNumbers.Where(nb => !dbNBs.Contains(nb)).OrderBy(nb => nb).ToList();
        }
    }
}
