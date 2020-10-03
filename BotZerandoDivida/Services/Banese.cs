using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Data;

using System.IO;
using System.Text;
using OfficeOpenXml;


namespace BotZerandoDivida.Services
{
    public class Banese
    {

        public static Pessoa ConsultarCpfCnpj(string cpfCnpj)
        {
            Pessoa pessoa = new Pessoa();
            string sFileName = @"Files\Devedores.xlsx";
            FileInfo file = new FileInfo(sFileName);
            try
            {
                using (ExcelPackage package = new ExcelPackage(file))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
                    int rowCount = worksheet.Dimension.Rows;
                    int ColCount = worksheet.Dimension.Columns;
                    for (int row = 2; row <= rowCount; row++)
                    {
                        if (worksheet.Cells[row, 1].Value.ToString().Equals(cpfCnpj))
                        {
                            pessoa.CpfCnpj = worksheet.Cells[row, 1].Value.ToString();
                            pessoa.Nome = worksheet.Cells[row, 2].Value.ToString();
                            pessoa.Valor_Divida = Convert.ToDecimal(worksheet.Cells[row, 3].Value.ToString());
                        }
                    }
                 
                }
            }
            catch (Exception ex)
            {
                
            }
            return pessoa;
        }


        public static List<string> CalcularParcela(decimal divida)
        {
            // Obter calculo exato
            List<String> parcelas = new List<string>();
            try
            {
                decimal parcela6x  = ((decimal)divida / (int)6)* (decimal)1.02;
                decimal parcela12x = ((decimal)divida / (int)12) * (decimal)1.03;
                decimal parcela24x = ((decimal)divida / (int)24) * (decimal)1.04;
                decimal parcela36x = ((decimal)divida / (int)36) * (decimal)1.05;

                parcelas.Add("6x  de " + parcela6x.ToString("C"));
                parcelas.Add("12x de " + parcela12x.ToString("C"));
                parcelas.Add("24x de " + parcela24x.ToString("C"));
                parcelas.Add("36x de " + parcela36x.ToString("C"));
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return parcelas;
        }


        public static void EnviarEmail(string email, string protocolo)
        {
                System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient();
                client.Host = "smtp.gmail.com";
                client.EnableSsl = true;
                client.Port = 587;
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential("saabarbosa@gmail.com", "i9ti.com.br");
                MailMessage mail = new MailMessage();
                mail.Sender = new System.Net.Mail.MailAddress("saabarbosa@gmail.com", "Zerando Dívida");
                mail.From = new MailAddress("chatbot@zerandodivida.com.br", "Zerando Dívida");
                mail.To.Add(new MailAddress(email, email));
                mail.Subject = "Zerando Dívida";
                mail.Body = "Olá, inicialmente agradecemos o interesse em resolver essa pendência.<br/>Seu contrato está em anexo. <br/>Imprima, assine e envie novamente para o nosso robô Stuart informando o número do protocolo:<strong>"+ protocolo +".</strong>";
                Attachment attach = new Attachment("CONTRATO.pdf");
                mail.Attachments.Add(attach);
                mail.IsBodyHtml = true;
                mail.Priority = MailPriority.High;
                try
                {
                    client.Send(mail);
                }
                catch (System.Exception erro)
                {
                    //trata erro
                }
                finally
                {
                    mail = null;
                }

        }

    }
}
