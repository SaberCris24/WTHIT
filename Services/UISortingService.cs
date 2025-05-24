using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace Plantilla.Services
{
    /// <summary>
    /// Servicio para manejar el estado y la lógica de ordenamiento en la UI
    /// </summary>
    public interface IUISortingService
    {
        /// <summary>
        /// Obtiene o establece si el ordenamiento por nombre está en orden ascendente
        /// </summary>
        bool IsNameSortAscending { get; set; }

        /// <summary>
        /// Obtiene o establece si el ordenamiento por ID está en orden ascendente
        /// </summary>
        bool IsIdSortAscending { get; set; }

        /// <summary>
        /// Actualiza el ícono de ordenamiento según el estado actual
        /// </summary>
        void UpdateSortIcon(FontIcon icon, bool isAscending);

        /// <summary>
        /// Obtiene el glifo correspondiente al estado de ordenamiento
        /// </summary>
        string GetSortGlyph(bool isAscending);

        /// <summary>
        /// Alterna el estado de ordenamiento y actualiza el ícono
        /// </summary>
        bool ToggleSortState(string sortType, FontIcon icon);
    }

    public class UISortingService : IUISortingService
    {
        private const string ASCENDING_GLYPH = "\uE70D";
        private const string DESCENDING_GLYPH = "\uE70E";

        public bool IsNameSortAscending { get; set; } = true;
        public bool IsIdSortAscending { get; set; } = true;

        /// <summary>
        /// Actualiza el ícono de ordenamiento según el estado
        /// </summary>
        public void UpdateSortIcon(FontIcon icon, bool isAscending)
        {
            if (icon != null)
            {
                icon.Glyph = GetSortGlyph(isAscending);
            }
        }

        /// <summary>
        /// Obtiene el glifo correspondiente al estado de ordenamiento
        /// </summary>
        public string GetSortGlyph(bool isAscending)
        {
            return isAscending ? ASCENDING_GLYPH : DESCENDING_GLYPH;
        }

        /// <summary>
        /// Alterna el estado de ordenamiento y actualiza el ícono
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