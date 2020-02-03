using asistencia.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace asistencia.Controllers
{
    public class LoginController : Controller
    {
        // GET: Login
        public ActionResult Index()
        {
            Session["fechaActual"] = null;
            Session["nombreMes"] = null;
            Session["Nombres"] = null;
            Session["perfil"] = null;
            Session["Id"] = null;
            Session["mes"] = null;
            Session["year"] = null;
            Session["listaHistorial"] = null;
            Session["listaEmpleados"] = null;

            return View();
        }

        public string Login(UsuariosCLS oUsuarioCLS)
        {
            string mensaje = "";
            string usuario = oUsuarioCLS.usuario;
            string password = oUsuarioCLS.password;
            try
            {

                    //inicio encriptado password
                    SHA256Managed sha = new SHA256Managed();
                    byte[] byteContra = Encoding.Default.GetBytes(password);
                    byte[] byteContraCifrado = sha.ComputeHash(byteContra);
                    string cadenaContraCifrada = BitConverter.ToString(byteContraCifrado).Replace("-", "");
                    password = cadenaContraCifrada;
                    //fin encriptado password

                    //CONEXION BASE DE DATOS
                    using (var bd = new asistenciaEntities())
                    {
                        //VERIFICAR SI EXISTE EL USUARIO EN BASE DE DATOS
                        int numeroVeces = bd.usuarios.Where(p => p.USU_USUARIO == usuario
                                                            && p.USU_PASSWORD == password).Count();
                        mensaje = numeroVeces.ToString();
                        //CONDICIÓN PARA INGRESAR AL SISTEMA
                        if(mensaje == "1")
                        {
                        //OBTENER FECHA ACTUAL
                            DateTime fecha = DateTime.Now.Date;
                            Session["fechaActual"] = fecha.ToString("yyyy-MM-dd");
                        //OBTENER EL NOMBRE DEL MES ACTUAL
                        DateTimeFormatInfo formatoFecha = CultureInfo.CurrentCulture.DateTimeFormat;
                        String mesNombre = formatoFecha.GetMonthName(DateTime.Now.Month);
                        Session["nombreMes"] = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(mesNombre);

                        //OBTENER INFORMACIÓN DEL USUARIO QUE SE LOGEA
                        usuarios oUsuario = bd.usuarios.Where(p => p.USU_USUARIO == usuario).First();
                            Session["Nombres"] = oUsuario.USU_NOMBRES;
                            Session["perfil"] = oUsuario.USU_PERFIL;
                            Session["Id"] = oUsuario.USU_ID;
                            Session["mes"] = DateTime.Now.Month;
                            Session["year"] = DateTime.Now.Year;

                        }
                        if (mensaje == "0") mensaje = "Usuario o contraseña incorrecta";
                    }
                    return mensaje;
                
            }
            catch (Exception e)
            {
                mensaje = "ERROR: " + e;
                return mensaje;
            }
                
        }
    }
}