using System;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using SiteConsumoAPINasa.HttpClients;
using SiteConsumoAPINasa.Models;

namespace SiteConsumoAPINasa.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;
        private readonly IImagemDiariaAPI _apiImagemDiaria;
        private readonly ConnectionMultiplexer _redisConnection;
        public string Saudacao { get; set; }
        public DateTime DataConsulta { get; set; }
        public InfoImagemNASA ImagemDiariaNASA { get; set; }

        public IndexModel(ILogger<IndexModel> logger,
            IConfiguration configuration,
            IMemoryCache memoryCache,
            IImagemDiariaAPI apiImagemDiaria,
            ConnectionMultiplexer redisConnection)
        {
            _logger = logger;
            _configuration = configuration;
            _memoryCache = memoryCache;
            _apiImagemDiaria = apiImagemDiaria;
            _redisConnection = redisConnection;
        }

        public void OnGet()
        {
            Saudacao = _configuration["Saudacao"];

            DataConsulta = DateTime.Now.Date.AddDays(
                new Random().Next(0, 7) * -1);
            string dataHttpRequest = $"{DataConsulta:yyyy-MM-dd}";

            string imagemKey = $"InfoImagemNASA-{dataHttpRequest}";
            var dbRedis = _redisConnection.GetDatabase();

            if (!dbRedis.HashExists(imagemKey, "Url"))
            {
                var infoImagem = _apiImagemDiaria.GetInfo(
                    _configuration["APIKeyNASA"], dataHttpRequest).Result;
                ImagemDiariaNASA = infoImagem;

                dbRedis.HashSet(imagemKey, new HashEntry[]
                {
                    new HashEntry("Date", infoImagem.Date),
                    new HashEntry("Explanation", infoImagem.Explanation),
                    new HashEntry("Media_type", infoImagem.Media_type),
                    new HashEntry("Title", infoImagem.Title),
                    new HashEntry("Url", infoImagem.Url),
                });
                
                _logger.LogInformation(
                    $"Carregadas informacoes para imagem do dia {DataConsulta:dd/MM/yyyy}: {infoImagem.Title}");
            }
            else
            {
                var infoImagemNASA = new InfoImagemNASA();
                infoImagemNASA.Date = dbRedis.HashGet(imagemKey, "Date");
                infoImagemNASA.Explanation = dbRedis.HashGet(imagemKey, "Explanation");
                infoImagemNASA.Media_type = dbRedis.HashGet(imagemKey, "Media_type");
                infoImagemNASA.Title = dbRedis.HashGet(imagemKey, "Title");
                infoImagemNASA.Url = dbRedis.HashGet(imagemKey, "Url");
 
                ImagemDiariaNASA = infoImagemNASA;
                _logger.LogInformation(
                    $"Utilizado cache da consulta a imagem do dia {DataConsulta:dd/MM/yyyy}: {ImagemDiariaNASA.Title}");
            }
        }
    }
}