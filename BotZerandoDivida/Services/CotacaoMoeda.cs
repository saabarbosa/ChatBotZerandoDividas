using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;

namespace CoreBot.Services
{
    [Serializable]
    public class CotacaoMoeda
    {
        HttpClient client = new HttpClient();
        public async Task<dynamic> Consultar(string url)
        {
            try
            {
                var response = await client.GetStringAsync("https://economia.awesomeapi.com.br/{url}");
                return JsonConvert.DeserializeObject(response);
                
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //public async Task<IEnumerable<Model.Cotacao>> Cotacao(string moeda)
        //{
        //    var list = new List<Model.Cotacao>();
        //    var siglaMoeda = string.Empty;
            
        //    if (!string.IsNullOrEmpty(moeda))
        //    {
        //        switch (moeda)
        //        {
        //            default:
        //                siglaMoeda = "USD-BRL";
        //                break;
        //        }
                
        //    }
        //    var result = await Consultar("jsonp/{siglaMoeda}/1?callback=jsonp_callback");
        //    foreach(var cotacao in result.valores)
        //    {
        //        var js = new DataContractJsonSerializer(typeof(Model.Cotacao));
        //        //var ms = new MemoryStream(Encoding.UTF8.GetBytes(((Newtonsoft.Json.Linq.JContainer)
        //        Model.Cotacao cot = new Model.Cotacao();
        //        cot.varBid = "0.0053";
        //        cot.code = "USD"; //,"codein":"BRL","name":"Dólar Comercial","high":"3.9728","low":"3.9282","pctChange":"0.14","bid":"3.9506","ask":"3.9513","timestamp":"1557519451","create_date":"2019-05-10 17:17:32"}]
        //        cot.name = "Dólar Comercial";
        //        cot.high = "3.9728";
        //        cot.low  = "3.9282";
        //        list.Add(cot);
        //    }
        //    return list;
        //}

    }
}
