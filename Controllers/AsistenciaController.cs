using asistencia.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace asistencia.Controllers
{
    public class AsistenciaController : Controller
    {
        // GET: Asistencia
        public ActionResult Index()
        {
            //condicion si existe un ID de usuario al hacer login
            if (Session["Id"] == null)
            {
                return RedirectToAction("Index", "Login");
            }
            if (!(Session["perfil"].Equals("Administrador")))
            {
                return RedirectToAction("Index", "Login");
            }
            List<AsistenciaCLS> listaAsistencia = new List<AsistenciaCLS>();
            using(var bd = new asistenciaEntities())
            {
                listaAsistencia = (from historial in bd.asistencia
                                   join usuario in bd.usuarios
                                   on historial.USU_ID equals usuario.USU_ID
                                   orderby historial.ASI_FECHA descending
                                   select new AsistenciaCLS
                                   {
                                       idAsistencia = historial.ASI_ID,
                                       empleadoAsistencia = usuario.USU_NOMBRES,
                                       fecha = (DateTime) historial.ASI_FECHA,
                                       horaIngreso = historial.ASI_HORA_INGRESO,
                                       horaSalida = historial.ASI_HORA_SALIDA,
                                       estadoAsistencia = (int) historial.ASI_ESTADO,
                                       observacion = historial.ASI_OBSERVACION,
                                       atrasos = (int) historial.ASI_ATRASO
                                   }).ToList();
                Session["listaHistorial"] = listaAsistencia;
            }
            return View(listaAsistencia);
        }

        public ActionResult Filtrar(AsistenciaCLS oAsistenciaCLS)
        {
            string termino = oAsistenciaCLS.termino;
            List<AsistenciaCLS> listaAsistencia = new List<AsistenciaCLS>();

            using (var bd = new asistenciaEntities())
            {
                if (termino == null)
                {
                    listaAsistencia = (from historial in bd.asistencia
                                       join usuario in bd.usuarios
                                       on historial.USU_ID equals usuario.USU_ID
                                       orderby historial.ASI_FECHA descending
                                       select new AsistenciaCLS
                                       {
                                           idAsistencia = historial.ASI_ID,
                                           empleadoAsistencia = usuario.USU_NOMBRES,
                                           fecha = (DateTime)historial.ASI_FECHA,
                                           horaIngreso = historial.ASI_HORA_INGRESO,
                                           horaSalida = historial.ASI_HORA_SALIDA,
                                           estadoAsistencia = (int)historial.ASI_ESTADO,
                                           observacion = historial.ASI_OBSERVACION
                                       }).ToList();
                    Session["listaHistorial"] = listaAsistencia;
                }
                else
                {
                    listaAsistencia = (from historial in bd.asistencia
                                       join usuario in bd.usuarios
                                       on historial.USU_ID equals usuario.USU_ID
                                       where usuario.USU_NOMBRES.Contains(termino)
                                       orderby historial.ASI_FECHA descending
                                       select new AsistenciaCLS
                                       {
                                           idAsistencia = historial.ASI_ID,
                                           empleadoAsistencia = usuario.USU_NOMBRES,
                                           fecha = (DateTime)historial.ASI_FECHA,
                                           horaIngreso = historial.ASI_HORA_INGRESO,
                                           horaSalida = historial.ASI_HORA_SALIDA,
                                           estadoAsistencia = (int)historial.ASI_ESTADO,
                                           observacion = historial.ASI_OBSERVACION
                                       }).ToList();
                    Session["listaHistorial"] = listaAsistencia;
                }

            }

            return PartialView("_tablaAsistencia", listaAsistencia);
        }

        //Editar justificación
        public JsonResult recuperarDatos(int id)
        {
            AsistenciaCLS oAsistenciaCLS = new AsistenciaCLS();
            try
            {
                using (var bd = new asistenciaEntities())
                {
                    Models.asistencia oAsistencia = bd.asistencia.Where(p => p.ASI_ID == id).First();
                    oAsistenciaCLS.observacion = oAsistencia.ASI_OBSERVACION;
                }
            }
            catch (Exception)
            {

                throw;
            }
            
            return Json(oAsistenciaCLS, JsonRequestBehavior.AllowGet);
        }

        public FileResult generarPDF()
        {
            Document doc = new Document();
            doc.SetMargins(-5, -5, 5, 5);
            byte[] buffer;

            using (MemoryStream ms = new MemoryStream())
            {
                PdfWriter.GetInstance(doc, ms);

                doc.Open();

                //Titulo del documento
                Paragraph title = new Paragraph("REPORTE DE ASISTENCIA");
                title.Alignment = Element.ALIGN_CENTER;
                doc.Add(title);

                Paragraph mesActual = new Paragraph("Fecha: " + Session["fechaActual"]);
                mesActual.Alignment = Element.ALIGN_CENTER;
                doc.Add(mesActual);

                Paragraph espacio = new Paragraph(" ");
                doc.Add(espacio);

                //tabla y columnas
                PdfPTable table = new PdfPTable(5);

                //Definición de ancho de columnas
                float[] values = new float[5] { 50, 100, 50, 50, 50 };
                table.SetWidths(values);

                //Creando celdas
                //celda 1
                PdfPCell celda1 = new PdfPCell(new Phrase("Fecha"));
                celda1.BackgroundColor = new BaseColor(130, 130, 130);
                celda1.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                table.AddCell(celda1);

                //celda 2
                PdfPCell celda2 = new PdfPCell(new Phrase("Empleado"));
                celda2.BackgroundColor = new BaseColor(130, 130, 130);
                celda2.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                table.AddCell(celda2);

                //celda 3
                PdfPCell celda3 = new PdfPCell(new Phrase("Hora Ingreso"));
                celda3.BackgroundColor = new BaseColor(130, 130, 130);
                celda3.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                table.AddCell(celda3);

                //celda 4
                PdfPCell celda4 = new PdfPCell(new Phrase("Hora Salida"));
                celda4.BackgroundColor = new BaseColor(130, 130, 130);
                celda4.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                table.AddCell(celda4);

                //celda 5
                PdfPCell celda5 = new PdfPCell(new Phrase("Justificación"));
                celda5.BackgroundColor = new BaseColor(130, 130, 130);
                celda5.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                table.AddCell(celda5);

                //Agregando contenido a la tabla
                List<AsistenciaCLS> lista = (List<AsistenciaCLS>)Session["listaHistorial"];

                int nregistros = lista.Count;
                for (int i = 0; i < nregistros; i++)
                {
                    PdfPCell celda = new PdfPCell(new Phrase(lista[i].fecha.ToString("dd-MM-yyyy")));
                    celda.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                    table.AddCell(celda);

                    celda = new PdfPCell(new Phrase(lista[i].empleadoAsistencia));
                    table.AddCell(celda);

                    celda = new PdfPCell(new Phrase(lista[i].horaIngreso.ToString()));
                    celda.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                    table.AddCell(celda);

                    celda = new PdfPCell(new Phrase(lista[i].horaSalida.ToString()));
                    celda.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                    table.AddCell(celda);

                    celda = new PdfPCell(new Phrase(lista[i].observacion));
                    celda.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                    table.AddCell(celda);

                }

                //Agregar tabla al documento
                doc.Add(table);
                doc.Close();

                buffer = ms.ToArray();

            }

            return File(buffer, "application/pdf");
        }

        public int aprobar(int id)
        {
            int ok = 0;

            
                using (var bd = new asistenciaEntities())
                {
                    Models.asistencia infoAsistencia = bd.asistencia.Where(p => p.ASI_ID.Equals(id)).First();
                    infoAsistencia.ASI_ESTADO = 1;
                    infoAsistencia.ASI_ATRASO = 1;
                    ok = bd.SaveChanges();
                }
                
            

            return ok;
        }

        public int justificarAsistencia(AsistenciaCLS oAsistenciaCLS, int titulo)
        {
            int ok = 0;
            
            using (var bd = new asistenciaEntities())
            {
                Models.asistencia asi = bd.asistencia.Where(p => p.ASI_ID.Equals(titulo)).First();

                asi.ASI_OBSERVACION = oAsistenciaCLS.observacion;
                ok = bd.SaveChanges();
            }

                return ok;
        }
    }
}