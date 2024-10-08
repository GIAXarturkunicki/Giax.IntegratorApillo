using Giax.IntegratorApillo.workers;
using Soneta.Business;
using Soneta.Business.UI;
using Soneta.Handel;
using System;
using System.Net.Http;

[assembly: Worker(typeof(IntegratorWorker), typeof(DokHandlowe))]

namespace Giax.IntegratorApillo.workers
{
    public class IntegratorWorker 
    {

        [Context]
        public IntegratorWorkerParams @params
        {
            get;
            set;
        }

        [Context]
        public Session session { get; set; }

        


        // TODO -> Należy podmienić podany opis akcji na bardziej czytelny dla uzytkownika
        [Action("IntegratorWorker/ToDo", Mode = ActionMode.SingleSession | ActionMode.ConfirmSave | ActionMode.Progress)]
        public MessageBoxInformation ToDo()
        {


            return new MessageBoxInformation("Potwierdzasz wykonanie operacji ?")
            {
                Text = "Opis operacji",
                YesHandler = () =>
                {
                    using (var t = @params.Session.Logout(true))
                    {
                        t.Commit();
                    }
                    return "Operacja została zakończona";
                },
                NoHandler = () => "Operacja przerwana"
            };

        }


        private void GetOrdersList()
        {
            //1. Call API for orders list
            using(HttpClient client = new HttpClient())
            {

            }
        }
    }


    public class IntegratorWorkerParams : ContextBase
    {
        public IntegratorWorkerParams(Context context) : base(context)
        {
        }

        // TODO -> Poniższy parametr dodany dla celów poglądowych. Należy usunąć.
        public string Parametr1 { get; set; }
    }

}
