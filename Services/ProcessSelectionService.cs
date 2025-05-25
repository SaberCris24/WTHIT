using System.Collections.Generic;
using System.Linq;
using Plantilla.Models;

namespace Plantilla.Services
{
    /// <summary>
    /// Servicio que maneja la selección y el estado de los procesos en la aplicación.
    /// Centraliza la lógica de selección y mantiene el estado consistente.
    /// </summary>
    public interface IProcessSelectionService
    {
        /// <summary>
        /// Verifica si hay algún proceso seleccionado en la colección proporcionada
        /// </summary>
        bool HasSelectedProcesses(IEnumerable<ProcessItem> processes);

        /// <summary>
        /// Obtiene todos los procesos seleccionados de la colección
        /// </summary>
        List<ProcessItem> GetSelectedProcesses(IEnumerable<ProcessItem> processes);

        /// <summary>
        /// Actualiza el estado de selección de un proceso específico
        /// </summary>
        void UpdateProcessSelection(ProcessItem process, bool isSelected);

        /// <summary>
        /// Limpia todas las selecciones en la colección de procesos
        /// </summary>
        void ClearSelections(IEnumerable<ProcessItem> processes);
        
        /// <summary>
        /// Sincroniza el estado de selección entre dos colecciones de procesos
        /// </summary>
        void SyncSelectionState(IEnumerable<ProcessItem> source, IEnumerable<ProcessItem> target);
    }

    public class ProcessSelectionService : IProcessSelectionService
    {
        /// <summary>
        /// Verifica si hay algún proceso seleccionado en la colección
        /// </summary>
        /// <param name="processes">Colección de procesos a verificar</param>
        /// <returns>True si hay al menos un proceso seleccionado</returns>
        public bool HasSelectedProcesses(IEnumerable<ProcessItem> processes)
        {
            return processes?.Any(p => p.IsSelected) ?? false;
        }

        /// <summary>
        /// Obtiene la lista de procesos seleccionados
        /// </summary>
        /// <param name="processes">Colección de procesos a filtrar</param>
        /// <returns>Lista de procesos seleccionados</returns>
        public List<ProcessItem> GetSelectedProcesses(IEnumerable<ProcessItem> processes)
        {
            return processes?.Where(p => p.IsSelected).ToList() ?? new List<ProcessItem>();
        }

        /// <summary>
        /// Actualiza el estado de selección de un proceso
        /// </summary>
        /// <param name="process">Proceso a actualizar</param>
        /// <param name="isSelected">Nuevo estado de selección</param>
        public void UpdateProcessSelection(ProcessItem process, bool isSelected)
        {
            if (process != null)
            {
                process.IsSelected = isSelected;
            }
        }

        /// <summary>
        /// Limpia todas las selecciones en la colección
        /// </summary>
        /// <param name="processes">Colección de procesos a limpiar</param>
        public void ClearSelections(IEnumerable<ProcessItem> processes)
        {
            if (processes == null) return;

            foreach (var process in processes)
            {
                process.IsSelected = false;
            }
        }

        /// <summary>
        /// Sincroniza el estado de selección entre dos colecciones
        /// </summary>
        /// <param name="source">Colección fuente</param>
        /// <param name="target">Colección destino</param>
        public void SyncSelectionState(IEnumerable<ProcessItem> source, IEnumerable<ProcessItem> target)
        {
            if (source == null || target == null) return;

            var selectedIds = source.Where(p => p.IsSelected)
                                  .Select(p => p.ProcessId)
                                  .ToHashSet();

            foreach (var process in target)
            {
                process.IsSelected = selectedIds.Contains(process.ProcessId);
            }
        }
    }
}