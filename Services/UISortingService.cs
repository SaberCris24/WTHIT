using Microsoft.UI.Xaml.Controls;
using System;

namespace Plantilla.Services
{
    /// <summary>
    /// Service to handle UI sorting state and logic
    /// </summary>
    public interface IUISortingService
    {
        /// <summary>
        /// Gets or sets whether name sorting is in ascending order
        /// </summary>
        bool IsNameSortAscending { get; set; }

        /// <summary>
        /// Gets or sets whether ID sorting is in ascending order
        /// </summary>
        bool IsIdSortAscending { get; set; }

        /// <summary>
        /// Updates the sort icon based on current state
        /// </summary>
        void UpdateSortIcon(FontIcon icon, bool isAscending);

        /// <summary>
        /// Gets the glyph for the current sort state
        /// </summary>
        string GetSortGlyph(bool isAscending);

        /// <summary>
        /// Toggles sort state and updates the icon
        /// </summary>
        bool ToggleSortState(string sortType, FontIcon icon);
    }

    /// <summary>
    /// Handles sorting UI state and interactions
    /// </summary>
    public class UISortingService : IUISortingService
    {
        // Glyph constants for sort icons
        private const string ASCENDING_GLYPH = "\uE70D";
        private const string DESCENDING_GLYPH = "\uE70E";

        // Default sort states
        public bool IsNameSortAscending { get; set; } = true;
        public bool IsIdSortAscending { get; set; } = true;

        /// <summary>
        /// Updates the sort icon based on direction
        /// </summary>
        public void UpdateSortIcon(FontIcon icon, bool isAscending)
        {
            if (icon != null)
            {
                icon.Glyph = GetSortGlyph(isAscending);
            }
        }

        /// <summary>
        /// Gets the appropriate sort glyph
        /// </summary>
        public string GetSortGlyph(bool isAscending)
        {
            return isAscending ? ASCENDING_GLYPH : DESCENDING_GLYPH;
        }

        /// <summary>
        /// Toggles sort state and updates icon
        /// </summary>
        public bool ToggleSortState(string sortType, FontIcon icon)
        {
            bool newState;
            
            switch (sortType.ToLower())
            {
                case "name":
                    IsNameSortAscending = !IsNameSortAscending;
                    newState = IsNameSortAscending;
                    break;
                case "id":
                    IsIdSortAscending = !IsIdSortAscending;
                    newState = IsIdSortAscending;
                    break;
                default:
                    throw new ArgumentException($"Invalid sort type: {sortType}");
            }

            UpdateSortIcon(icon, newState);
            return newState;
        }
    }
}