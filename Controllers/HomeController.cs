using asistencia.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace asistencia.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            //condicion si existe un ID de usuario al hacer login
            
            if(Session["Id"] == null)
            {
                return RedirectToAction("Index","Login");
            }
            //guardarmos variables de session luego de hacer login en variables locales
            int idUsuarioSession = (int) Session["Id"];
            DateTime fechaSession = DateTime.Parse(Session["fechaActual"].ToString());

            List<UsuariosCLS> ListaUsuarios = new List<UsuariosCLS>();
            AsistenciaCLS asisCLS = new AsistenciaCLS();
  
                using (var bd = new asistenciaEntities())
                {
                    //CONSULTANDO EN BASE DE DATOS SI HAY REGISTROS CON LA FECHA DE HOY
                    int numeroVeces = bd.asistencia.Where(p => p.USU_ID.Equals(idUsuarioSession)
                                                        && p.ASI_FECHA.Equals(fechaSession)).Count();
                    //SI NO HAY REGISTRO INGRESA PROCEDE A REGISTRAR
                    if (numeroVeces < 1)
                    {
                        //RECUPERANDO TODOS LOS USUARIOS
                        ListaUsuarios = (from usu in bd.usuarios
                                         where usu.USU_ESTADO == 1
                                         select new UsuariosCLS
                                         {
                                             idUsuario = usu.USU_ID
                                         }).ToList();

                        //AGREGANDO REGISTRO DE FECHA DIARIA POR USUARIO A HISTORIAL
                        foreach (UsuariosCLS usuario in ListaUsuarios)
                        {
                            Models.asistencia asi = new Models.asistencia();
                            asi.USU_ID = usuario.idUsuario;
                            asi.ASI_FECHA = fechaSession;
                            asi.ASI_ESTADO = 0;
                            asi.ASI_ATRASO = 0;
                            bd.asistencia.Add(asi);
                            bd.SaveChanges();
                        }                        

                    }
                    //Actualizar total Atrasos por mes
                    if (Session["perfil"].Equals("Administrador"))
                    {
                        ListaUsuarios = (from usu in bd.usuarios
                                         select new UsuariosCLS
                                         {
                                             idUsuario = usu.USU_ID
                                         }).ToList();
                        foreach (UsuariosCLS usuario in ListaUsuarios)
                        {
                            //Metodo registrrar total atrasos por usuarioz
                            Atrasos(usuario.idUsuario);
                        }
                    }
                    //Recuperando información del registro que sea igual al login
                    Models.asistencia asistenciaUsuario = bd.asistencia.Where(p => p.USU_ID.Equals(idUsuarioSession)
                                                                            && p.ASI_FECHA.Equals(fechaSession)).First();

                    asisCLS.horaIngreso = asistenciaUsuario.ASI_HORA_INGRESO;
                    asisCLS.horaSalida = asistenciaUsuario.ASI_HORA_SALIDA;
                }
  
            
                return View(asisCLS);
        }

        //Contar numero de atrasos por usuario id
        public void Atrasos(int id)
        {
            int cero = 0;
            int mes = (int) Session["mes"];
            int year = (int) Session["year"];
            int numAtrasos;
            usuarios usu = new usuarios();
            using (var bd = new asistenciaEntities())
            {
                numAtrasos = (from asist in bd.asistencia
                                where
                                asist.USU_ID == id
                                && asist.ASI_ATRASO == cero
                                && SqlFunctions.DatePart("month", asist.ASI_FECHA) == mes
                                && SqlFunctions.DatePart("year", asist.ASI_FECHA) == year    
                                select asist).Count();

                usu = bd.usuarios.Where(p => p.USU_ID.Equals(id)).First();
                usu.USU_TOTAL_ATRASOS_MES = numAtrasos;
                bd.SaveChanges();
            }
        }

        public string IngresoHora()
        {
            //Declaración de variables
            int opcion = 1;

            //HORA ESTABLECIDA POR LA EMPRESA 9:10 COMO MAXIMO, POSTERIOR A ESO CUENTA COMO ATRASO
            TimeSpan horaEstablecida = new TimeSpan(09, 10, 00);

            //INFORMACION DEL USUARIO LOGEADO
            int idUsuarioSession = (int)Session["Id"];
            DateTime fechaSession = DateTime.Parse(Session["fechaActual"].ToString());
            fechaSession = fechaSession.Date;

            //GENERACIÓN DE HORA DEL MOMENTO
            TimeSpan hora = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            String ingreso = hora.ToString();

            //REGISTRO HORA EN BASE DE DATOS DEL USUARIO LOGEADO
            Models.asistencia asi = new Models.asistencia();
            using (var bd = new asistenciaEntities())
            {

                asi = (from asist in bd.asistencia
                      where asist.USU_ID.Equals(idUsuarioSession)
                      && asist.ASI_FECHA.Equals(fechaSession)
                      select asist).First();

                asi.ASI_HORA_INGRESO = hora;
                asi.ASI_ESTADO = 1;

                if(hora< horaEstablecida)
                {
                    asi.ASI_ATRASO = 1;
                }

                bd.SaveChanges();

                
            }
            LogicaHoras(idUsuarioSession, hora, opcion);
            return ingreso;
        }

        public string SalidaHora()
        {
            int ok = 0;
            int opcion = 0;
            int numSalida = 0;
            //INFORMACION DEL USUARIO LOGEADO
            int idUsuarioSession = (int)Session["Id"];
            DateTime fechaSession = DateTime.Parse(Session["fechaActual"].ToString());
            fechaSession = fechaSession.Date;

            //GENERACIÓN DE HORA DEL MOMENTO
            TimeSpan hora = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            String salida = hora.ToString();

            //REGISTRO HORA EN BASE DE DATOS DEL USUARIO LOGEADO
            Models.asistencia asi = new Models.asistencia();
            using (var bd = new asistenciaEntities())
            {

                asi = (from asist in bd.asistencia
                       where asist.USU_ID.Equals(idUsuarioSession)
                       && asist.ASI_FECHA.Equals(fechaSession)
                       select asist).First();

                asi.ASI_HORA_SALIDA = hora;
                ok = bd.SaveChanges();
            }
            LogicaHoras(idUsuarioSession, hora, opcion);
            return salida;
        }

        public int justificar(AsistenciaCLS asistenciaCLS)
        {
            int rpta = 0;
            int idUsuarioSession = (int)Session["Id"];
            try
            {
                using (var bd = new asistenciaEntities())
                {
                    Models.asistencia asi = bd.asistencia.Where(p => p.USU_ID.Equals(idUsuarioSession)
                                                                && p.ASI_FECHA.Equals(asistenciaCLS.fechaJustificar)).First();
                    asi.ASI_OBSERVACION = asistenciaCLS.observacion;
                    rpta = bd.SaveChanges();
                }
            }
            catch (Exception)
            {

                throw;
            }
            return rpta;
        }

        public void LogicaHoras(int idUuser, TimeSpan hora,int opcion)
        {
            int diaU = 0;
            int horaU = 0;
            int minU = 0;
            int minDIF = 0;
            int horaDIF = 0;
            int horaTemp = horaU;
            TimeSpan horaEstablecida;
            TimeSpan diferenciaHora;
            TimeSpan cero = new TimeSpan(0, 0, 0, 0);

            if (opcion == 1)
            {
                horaEstablecida = new TimeSpan(09,00,00);
                diferenciaHora = horaEstablecida - hora;
            }
            else
            {
                horaEstablecida = new TimeSpan(17,30,00);
                diferenciaHora = hora - horaEstablecida;
            }

            horaDIF = diferenciaHora.Hours;
            minDIF = diferenciaHora.Minutes;

            usuarios usuario = new usuarios();
            using (var bd = new asistenciaEntities())
            {
                usuario = (from usu in bd.usuarios
                       where usu.USU_ID.Equals(idUuser)
                       select usu).First();

                diaU = (int)usuario.USU_DIA;
                horaU = (int)usuario.USU_HORA;
                minU = (int)usuario.USU_MIN;

                //Condicion suma horas
                if (diferenciaHora < cero)
                {
                    if (minU <= 0)
                    {
                        if ((minU + minDIF) <= -60)
                        {
                            minU = 60 + (minU + minDIF);
                            horaTemp = horaU - 1;
                            if (horaTemp <= -24)
                            {
                                diaU -= 1;
                                horaU = 0;
                            }
                            else
                            {
                                horaU -= 1;
                            }
                        }
                        else
                        {
                            minU += minDIF;
                        }
                    }
                    else
                    {
                        if ((minU + minDIF) < 0)
                        {
                            minU = (minU + minDIF);
                            horaTemp = horaU - 1;
                            if (horaTemp <= -24)
                            {
                                diaU -= 1;
                                horaU = 0;
                            }
                            else
                            {
                                horaU -= 1;
                            }
                        }
                        else
                        {
                            minU += minDIF;
                        }
                    }
                }
                else
                {
                    if (minU < 0)
                    {
                        if ((minU + minDIF) >= 0)
                        {
                            minU = minU + minDIF;
                            horaTemp = horaU + 1;
                            if (horaTemp >= 24)
                            {
                                diaU += 1;
                                horaU = 0;
                            }
                            else
                            {
                                horaU += 1;
                            }
                        }
                        else
                        {
                            minU += minDIF;
                        }
                    }
                    else
                    {
                        if ((minU + minDIF) >= 60)
                        {
                            minU = 60 - (minU + minDIF);
                            horaTemp = horaU + 1;
                            if (horaTemp >= 24)
                            {
                                diaU += 1;
                                horaU = 0;
                            }
                            else
                            {
                                horaU += 1;
                            }
                        }
                        else
                        {
                            minU += minDIF;
                        }
                    }
                }

                //Condicion suma horas
                if (diferenciaHora < cero)
                {

                    if (horaU <= 0)
                    {
                        if ((horaU + horaDIF) <= -24)
                        {
                            horaU = 24 + (horaU + horaDIF);
                            diaU = diaU - 1;
                        }
                        else
                        {
                            horaU += horaDIF;
                        }
                    }
                    else
                    {
                        if ((horaU + horaDIF) < 0)
                        {
                            horaU = (24 + (horaU + horaDIF)) * -1;
                            diaU = diaU - 1;
                        }
                        else
                        {
                            horaU += horaDIF;
                        }
                    }

                }
                else
                {

                    if (horaU < 0)
                    {
                        if ((horaU + horaDIF) >= 0)
                        {
                            horaU += horaDIF;
                            diaU += 1;
                        }
                        else
                        {
                            horaU += horaDIF;
                        }
                    }
                    else
                    {
                        if ((horaU + horaDIF) >= 24)
                        {
                            horaU = (horaU + horaDIF) - 24;
                            diaU += 1;
                        }
                        else
                        {
                            horaU += horaDIF;
                        }
                    }

                }

                usuario.USU_DIA = diaU;
                usuario.USU_HORA = horaU;
                usuario.USU_MIN = minU;

                bd.SaveChanges();
            }

        }

    }

}