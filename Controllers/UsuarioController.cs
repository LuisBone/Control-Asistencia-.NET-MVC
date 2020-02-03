using asistencia.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace asistencia.Controllers
{
    public class UsuarioController : Controller
    {

        // GET: Usuario
        public ActionResult Index()
        {
            int totalAtrasos = 0;
            //condicion si existe un ID de usuario al hacer login
            if (Session["Id"] == null)
            {
                return RedirectToAction("Index", "Login");
            }

            List<UsuariosCLS> listaUsuarios = new List<UsuariosCLS>();
            try
            {
                
                using (var bd = new asistenciaEntities())
                {
                   
                    listaUsuarios = (from usuarios in bd.usuarios
                                     orderby usuarios.USU_NOMBRES
                                     select new UsuariosCLS
                                     {
                                         idUsuario = usuarios.USU_ID,
                                         nombres = usuarios.USU_NOMBRES,
                                         usuario = usuarios.USU_USUARIO,
                                         perfil = usuarios.USU_PERFIL,
                                         estado = (int) usuarios.USU_ESTADO,
                                         dia = (int) usuarios.USU_DIA,
                                         hora = (int) usuarios.USU_HORA,
                                         min = (int) usuarios.USU_MIN,
                                         totalAtrasosMes = (int) usuarios.USU_TOTAL_ATRASOS_MES                                         
                                     }).ToList();
                    Session["listaEmpleados"] = listaUsuarios;
                }
                listaPerfiles();
                return View(listaUsuarios);
            }
            catch(Exception ex)
            {
                return View(ex);
            }
            
            
        }

        public ActionResult Filtrar(UsuariosCLS oUsuariosCLS)
        {
            string termino = oUsuariosCLS.termino;
            List<UsuariosCLS> listaUsuarios = new List<UsuariosCLS>();

            using (var bd = new asistenciaEntities())
            {
                if(termino == null)
                {
                    listaUsuarios = (from usuarios in bd.usuarios
                                     orderby usuarios.USU_NOMBRES
                                     select new UsuariosCLS
                                     {
                                         idUsuario = usuarios.USU_ID,
                                         nombres = usuarios.USU_NOMBRES,
                                         usuario = usuarios.USU_USUARIO,
                                         perfil = usuarios.USU_PERFIL,
                                         estado = (int) usuarios.USU_ESTADO,
                                         dia = (int) usuarios.USU_DIA,
                                         hora = (int) usuarios.USU_HORA,
                                         min = (int) usuarios.USU_MIN,
                                         totalAtrasosMes = (int) usuarios.USU_TOTAL_ATRASOS_MES
                                     }).ToList();
                    Session["listaEmpleados"] = listaUsuarios;
                }
                else
                {
                    listaUsuarios = (from usuarios in bd.usuarios
                                     where usuarios.USU_NOMBRES.Contains(termino)
                                     select new UsuariosCLS
                                     {
                                         idUsuario = usuarios.USU_ID,
                                         nombres = usuarios.USU_NOMBRES,
                                         usuario = usuarios.USU_USUARIO,
                                         perfil = usuarios.USU_PERFIL,
                                         estado = (int) usuarios.USU_ESTADO,
                                         dia = (int)usuarios.USU_DIA,
                                         hora = (int)usuarios.USU_HORA,
                                         min = (int)usuarios.USU_MIN,
                                         totalAtrasosMes = (int)usuarios.USU_TOTAL_ATRASOS_MES
                                     }).ToList();
                    Session["listaEmpleados"] = listaUsuarios;
                }
                
            }

            return PartialView("_tablaUsuarios", listaUsuarios);
        }

        public void listaPerfiles()
        {
            List<SelectListItem> listaPerfil = new List<SelectListItem>();
            listaPerfil.Insert(0, new SelectListItem { Text = "--Selecciones--", Value = "" });
            listaPerfil.Insert(1, new SelectListItem { Text = "Administrador", Value = "Administrador" });
            listaPerfil.Insert(2, new SelectListItem { Text = "Empleado", Value = "Empleado" });

            ViewBag.listaPerfil = listaPerfil;
        }

        public string Guardar(UsuariosCLS oUsuarioCLS, int titulo)
        {
            string rpta = "";

            try
            { 
                if (!ModelState.IsValid)
                {
                    var query = (from state in ModelState.Values
                                 from error in state.Errors
                                 select error.ErrorMessage).ToList();
                    rpta += "<p class='text-danger' style='padding: 10px'>CORREGIR:</p><ul class='list-group'>";
                    foreach (var item in query)
                    {
                        rpta += "<li style='color:red' class='list-group-item'>" + item + "</li>";
                    }
                    rpta += "</ul>";
                }
                else
                {
                    
                    using (var bd = new asistenciaEntities())
                    {
                        if (titulo.Equals(-1))
                        {
                            
                            usuarios oUsuario = new usuarios();
                            oUsuario.USU_ID = oUsuarioCLS.idUsuario;
                            oUsuario.USU_NOMBRES = oUsuarioCLS.nombres;
                            oUsuario.USU_USUARIO = oUsuarioCLS.usuario;

                            //inicio encriptado password
                            SHA256Managed sha = new SHA256Managed();
                            byte[] byteContra = Encoding.Default.GetBytes(oUsuarioCLS.password);
                            byte[] byteContraCifrado = sha.ComputeHash(byteContra);
                            string cadenaContraCifrada = BitConverter.ToString(byteContraCifrado).Replace("-", "");
                            oUsuario.USU_PASSWORD = cadenaContraCifrada;
                            //fin encriptado password

                            oUsuario.USU_PERFIL = oUsuarioCLS.perfil;
                            oUsuario.USU_ESTADO = 1;
                            oUsuario.USU_FECHA = DateTime.Now;
                            oUsuario.USU_DIA = 0;
                            oUsuario.USU_HORA = 0;
                            oUsuario.USU_MIN = 0;
                            oUsuario.USU_TOTAL_ATRASOS_MES = 0;

                            //Guardar en base de datos
                            bd.usuarios.Add(oUsuario);
                            rpta = bd.SaveChanges().ToString();

                            if (rpta == "0") rpta = "";

                        } 
                        else if(titulo >= 1)
                        {
                            usuarios oUsuario = bd.usuarios.Where(p => p.USU_ID == titulo).First();
                            oUsuario.USU_USUARIO = oUsuarioCLS.usuario;
                            oUsuario.USU_NOMBRES = oUsuarioCLS.nombres;

                            if(oUsuarioCLS.password != null) {
                                //inicio encriptado password
                                SHA256Managed sha = new SHA256Managed();
                                byte[] byteContra = Encoding.Default.GetBytes(oUsuarioCLS.password);
                                byte[] byteContraCifrado = sha.ComputeHash(byteContra);
                                string cadenaContraCifrada = BitConverter.ToString(byteContraCifrado).Replace("-", "");
                                oUsuario.USU_PASSWORD = cadenaContraCifrada;
                                //fin encriptado password
                            }

                            oUsuario.USU_PERFIL = oUsuarioCLS.perfil;

                            rpta = bd.SaveChanges().ToString();

                            if (rpta == "0") rpta = "";

                        }
                    }
                }

            }   catch (Exception ex)
            {
                return rpta = "Error"+ex;
            }

            return rpta;
        }



        public JsonResult recuperarDatos(int titulo)
        {
            UsuariosCLS oUsuarioCLS = new UsuariosCLS();
            try
            {
                using (var bd = new asistenciaEntities())
                {
                    usuarios oUsuario = bd.usuarios.Where(p => p.USU_ID == titulo).First();
                    oUsuarioCLS.idUsuario = oUsuario.USU_ID;
                    oUsuarioCLS.usuario = oUsuario.USU_USUARIO;
                    oUsuarioCLS.nombres = oUsuario.USU_NOMBRES;
                    oUsuarioCLS.perfil = oUsuario.USU_PERFIL;
                }
                
                return Json(oUsuarioCLS, JsonRequestBehavior.AllowGet);
            }
            catch(Exception e)
            {
                return Json(e, JsonRequestBehavior.AllowGet);
            }
            
        }

        public JsonResult eliminar(int titulo)
        {
            string del;
            try
            {
                using (var bd = new asistenciaEntities())
                {
                    usuarios oUsuario = bd.usuarios.Where(p => p.USU_ID == titulo).First();
                    del = bd.usuarios.Remove(oUsuario).ToString();
                    bd.SaveChanges();
                }
                return Json(del, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(e, JsonRequestBehavior.AllowGet);
            }

        }

        public FileResult generarPDF()
        {
            Document doc = new Document();
            byte[] buffer;

            using(MemoryStream ms = new MemoryStream())
            {
                PdfWriter.GetInstance(doc, ms);

                doc.Open();

                //Titulo del documento
                Paragraph title = new Paragraph("REPORTE DE ATRASOS DE LOS EMPLEADOS ");
                title.Alignment = Element.ALIGN_CENTER;
                doc.Add(title);

                Paragraph mesActual = new Paragraph("Mes "+ Session["nombreMes"]);
                mesActual.Alignment = Element.ALIGN_CENTER;
                doc.Add(mesActual);

                Paragraph espacio = new Paragraph(" ");
                doc.Add(espacio);

                //tabla y columnas
                PdfPTable table = new PdfPTable(4);

                //Definición de ancho de columnas
                float[] values = new float[4] { 15, 100, 40, 50 };
                table.SetWidths(values);

                //Creando celdas
                //celda 1
                PdfPCell celda1 = new PdfPCell(new Phrase("#"));
                celda1.BackgroundColor = new BaseColor(130, 130, 130);
                celda1.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                table.AddCell(celda1);

                //celda 2
                PdfPCell celda2 = new PdfPCell(new Phrase("Empleado"));
                celda2.BackgroundColor = new BaseColor(130, 130, 130);
                celda2.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                table.AddCell(celda2);

                //celda 3
                PdfPCell celda3 = new PdfPCell(new Phrase("Total atrasos"));
                celda3.BackgroundColor = new BaseColor(130, 130, 130);
                celda3.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                table.AddCell(celda3);

                //celda 4
                PdfPCell celda4 = new PdfPCell(new Phrase("% Descuento"));
                celda4.BackgroundColor = new BaseColor(130, 130, 130);
                celda4.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                table.AddCell(celda4);

                //Agregando contenido a la tabla
                List<UsuariosCLS> lista = (List<UsuariosCLS>) Session["listaEmpleados"];
                int nregistros = lista.Count;
                for(int i=0; i < nregistros; i++)
                {
                    PdfPCell celda = new PdfPCell(new Phrase((i + 1).ToString()));
                    celda.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                    table.AddCell(celda);

                    celda = new PdfPCell(new Phrase(lista[i].nombres));
                    table.AddCell(celda);

                    celda = new PdfPCell(new Phrase(lista[i].totalAtrasosMes.ToString()));
                    celda.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                    table.AddCell(celda);

                    if (lista[i].totalAtrasosMes > 3)
                    {
                        celda = new PdfPCell(new Phrase("10% de descuento"));
                        celda.BackgroundColor = new BaseColor(252, 132, 3);
                        celda.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                        table.AddCell(celda);
                    }
                    else
                    {
                        celda = new PdfPCell(new Phrase("No Aplica"));
                        celda.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                        table.AddCell(celda);
                    }
                }

                //Agregar tabla al documento
                doc.Add(table);
                doc.Close();

                buffer = ms.ToArray();

            }

            return File(buffer, "application/pdf");
        }


    }
}