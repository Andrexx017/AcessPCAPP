using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AppParque.Shared;
using AppParque.Services;
using System.Collections.Generic;

namespace AppParque.Features.Historial
{
    public class HistorialViewModel : BaseViewModel
    {
        private readonly FireBaseService _firebaseService = new FireBaseService();
        private List<TestRecord> _todosLosRegistros = new();

        public ObservableCollection<TestRecord> Historial { get; set; } = new();

        public HistorialViewModel()
        {
            _ = CargarHistorial();
        }

        private async Task CargarHistorial()
        {
            IsBusy = true;

            var uid = UsuarioGlobal.Uid;
            var role = UsuarioGlobal.Role;

            // 🔹 Cargar usuarios primero
            var users = await _firebaseService.GetUserAsync();

            var tests = await _firebaseService.GetTestsAsync(uid, role);

            Historial.Clear();
            _todosLosRegistros.Clear();

            if (tests != null)
            {
                foreach (var kv in tests) // kv.Key = uid del enfermero
                {
                    var enfermeroUid = kv.Key;

                    string nurseName = users != null && users.ContainsKey(enfermeroUid)
                        ? users[enfermeroUid].nurseName
                        : enfermeroUid;


                    foreach (var test in kv.Value.Values.Where(t => t != null))
                    {
                        var record = new TestRecord
                        {
                            VisitorName = test.name,
                            VisitorId = test.idNumber,
                            NurseName = nurseName,
                            Date = test.date,
                            Stature = test.stature,
                            Answers = test.answers
                        };

                        Historial.Add(record);
                        _todosLosRegistros.Add(record);
                    }
                }

            }

            IsBusy = false;
        }


        public void FiltrarHistorial(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                // Restaurar todos
                Historial.Clear();
                foreach (var item in _todosLosRegistros)
                    Historial.Add(item);
                return;
            }

            var filtrados = _todosLosRegistros.Where(r =>
                (r.VisitorName?.ToLower().Contains(query.ToLower()) ?? false) ||
                (r.VisitorId?.ToLower().Contains(query.ToLower()) ?? false));

            Historial.Clear();
            foreach (var item in filtrados)
                Historial.Add(item);
        }
    }
}
