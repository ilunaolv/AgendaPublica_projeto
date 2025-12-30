using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using WebApplication1.Models;
using Microsoft.AspNetCore.Http;

namespace WebApplication1.Controllers
{
    public class EventoController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public EventoController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public IActionResult TestarConexao()
        {
            ViewBag.ConnectionString = _connectionString;

            try
            {
                // Testar conexão
                using (var con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    ViewBag.ConexaoStatus = "✅ CONEXÃO OK";
                    ViewBag.Server = con.DataSource;
                    ViewBag.Database = con.Database;
                }

                // Testar cada método
                ViewBag.Secretarias = BuscarSecretarias();
                ViewBag.TiposEvento = BuscarTiposEvento();
                ViewBag.NiveisPrioridade = BuscarNiveisPrioridade();
                ViewBag.Bairros = BuscarBairros();

                // Contar registros
                ViewBag.SecretariasCount = ViewBag.Secretarias.Count;
                ViewBag.TiposEventoCount = ViewBag.TiposEvento.Count;
                ViewBag.NiveisPrioridadeCount = ViewBag.NiveisPrioridade.Count;
                ViewBag.BairrosCount = ViewBag.Bairros.Count;
            }
            catch (Exception ex)
            {
                ViewBag.ConexaoStatus = "❌ ERRO NA CONEXÃO";
                ViewBag.Error = ex.Message;
                ViewBag.StackTrace = ex.StackTrace;
            }

            return View();
        }

        [HttpGet]
        public IActionResult Criar()
        {
            ViewBag.Secretarias = BuscarSecretarias();
            ViewBag.TiposEvento = BuscarTiposEvento();
            ViewBag.NiveisPrioridade = BuscarNiveisPrioridade();
            ViewBag.Bairros = BuscarBairros();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Criar(EventoViewModel model, List<IFormFile> Documentos)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Secretarias = BuscarSecretarias();
                ViewBag.TiposEvento = BuscarTiposEvento();
                ViewBag.NiveisPrioridade = BuscarNiveisPrioridade();
                ViewBag.Bairros = BuscarBairros();
                return View(model);
            }

            int usuarioId = ObterUsuarioIdLogado();
            int? enderecoId = null; // Pode ser NULL
            int? documentoId = null; // Pode ser NULL
            int eventoId = 0;

            using (var con = new SqlConnection(_connectionString))
            {
                await con.OpenAsync();

                using (var transaction = con.BeginTransaction())
                {
                    try
                    {
                        // 1. PRIMEIRO: INSERIR ENDEREÇO (se houver dados)
                        if (!string.IsNullOrEmpty(model.Endereco?.Cep) ||
                            !string.IsNullOrEmpty(model.Endereco?.Endereco) ||
                            !string.IsNullOrEmpty(model.Endereco?.Bairro))
                        {
                            string sqlEndereco = @"
                                INSERT INTO dbo.Endereco 
                                (nrCep, dsLogradouro, nrLogradouro, dsBairro, dsComplemento, dtAtualizacaoRegistro)
                                OUTPUT INSERTED.cdEndereco
                                VALUES
                                (@cep, @logradouro, @numero, @bairro, @complemento, @dataAtualizacao)";

                            SqlCommand cmdEndereco = new SqlCommand(sqlEndereco, con, transaction);

                            cmdEndereco.Parameters.AddWithValue("@cep", model.Endereco?.Cep ?? "");
                            cmdEndereco.Parameters.AddWithValue("@logradouro", model.Endereco?.Endereco ?? "");
                            cmdEndereco.Parameters.AddWithValue("@numero", model.Endereco?.Numero ?? "");
                            cmdEndereco.Parameters.AddWithValue("@bairro", model.Endereco?.Bairro ?? "");
                            cmdEndereco.Parameters.AddWithValue("@complemento", model.Endereco?.Complemento ?? "");
                            cmdEndereco.Parameters.AddWithValue("@dataAtualizacao", DateTime.Now);

                            enderecoId = (int)await cmdEndereco.ExecuteScalarAsync();
                            Debug.WriteLine($"Endereço inserido com ID: {enderecoId}");
                        }

                        // 2. SALVAR DOCUMENTO (apenas o primeiro, se houver)
                        if (Documentos != null && Documentos.Count > 0 && Documentos[0] != null && Documentos[0].Length > 0)
                        {
                            var documento = Documentos[0];

                            if (documento.ContentType == "application/pdf" && documento.Length <= 5 * 1024 * 1024)
                            {
                                // Criar diretório se não existir
                                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documentos");
                                if (!Directory.Exists(uploadsFolder))
                                {
                                    Directory.CreateDirectory(uploadsFolder);
                                }

                                // Gerar nome único para o arquivo
                                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(documento.FileName);
                                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                // Salvar arquivo no servidor
                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await documento.CopyToAsync(fileStream);
                                }

                                // INSERIR DOCUMENTO NO BANCO
                                string sqlDocumento = @"
                                    INSERT INTO dbo.Documento 
                                    (nmArquivo, dsCaminho, dsTipoArquivo, dtUpload, dtAtualizacaoRegistro)
                                    OUTPUT INSERTED.cdDocumento
                                    VALUES
                                    (@nomeArquivo, @caminho, @tipoArquivo, @dataUpload, @dataAtualizacao)";

                                SqlCommand cmdDocumento = new SqlCommand(sqlDocumento, con, transaction);

                                cmdDocumento.Parameters.AddWithValue("@nomeArquivo", documento.FileName);
                                cmdDocumento.Parameters.AddWithValue("@caminho", $"/uploads/documentos/{uniqueFileName}");
                                cmdDocumento.Parameters.AddWithValue("@tipoArquivo", "PDF");
                                cmdDocumento.Parameters.AddWithValue("@dataUpload", DateTime.Now);
                                cmdDocumento.Parameters.AddWithValue("@dataAtualizacao", DateTime.Now);

                                documentoId = (int)await cmdDocumento.ExecuteScalarAsync();
                                Debug.WriteLine($"Documento inserido com ID: {documentoId}");
                            }
                        }

                        // 3. AGORA INSERIR O EVENTO (com todas as FKs)
                        string sqlEvento = @"
                            INSERT INTO dbo.Evento 
                            (nmTitulo, dsEvento, cdSecretaria, ckVisualizacao, cdTipoEvento, 
                             cdDocumento, dtInicio, cdUsuarioRegistrado, dtRegistro, dtFim, 
                             ckAtivo, cdUsuarioAtualizacao, dtAtualizacaoRegistro, cdEndereco, cdNivelPrioridade)
                            VALUES
                            (@titulo, @descricao, @secretaria, @visualizacao, @tipo, 
                             @documento, @inicio, @usuarioRegistrado, @registro, @fim, 
                             @ativo, @usuarioAtualizacao, @dataAtualizacao, @endereco, @nivelPrioridade)";

                        SqlCommand cmdEvento = new SqlCommand(sqlEvento, con, transaction);

                        cmdEvento.Parameters.AddWithValue("@titulo", model.Titulo);
                        cmdEvento.Parameters.AddWithValue("@descricao", model.Descricao ?? "");
                        cmdEvento.Parameters.AddWithValue("@secretaria", model.CdSecretaria);
                        cmdEvento.Parameters.AddWithValue("@visualizacao", model.VisualizacaoInterna);
                        cmdEvento.Parameters.AddWithValue("@tipo", model.CdTipoEvento);

                        // Documento pode ser NULL
                        if (documentoId.HasValue)
                            cmdEvento.Parameters.AddWithValue("@documento", documentoId.Value);
                        else
                            cmdEvento.Parameters.AddWithValue("@documento", DBNull.Value);

                        cmdEvento.Parameters.AddWithValue("@inicio", model.DataInicio);
                        cmdEvento.Parameters.AddWithValue("@usuarioRegistrado", usuarioId);
                        cmdEvento.Parameters.AddWithValue("@registro", DateTime.Now);
                        cmdEvento.Parameters.AddWithValue("@fim", model.DataFim);
                        cmdEvento.Parameters.AddWithValue("@ativo", true);
                        cmdEvento.Parameters.AddWithValue("@usuarioAtualizacao", usuarioId);
                        cmdEvento.Parameters.AddWithValue("@dataAtualizacao", DateTime.Now);

                        // Endereço pode ser NULL
                        if (enderecoId.HasValue)
                            cmdEvento.Parameters.AddWithValue("@endereco", enderecoId.Value);
                        else
                            cmdEvento.Parameters.AddWithValue("@endereco", DBNull.Value);

                        cmdEvento.Parameters.AddWithValue("@nivelPrioridade", model.CdNivelPrioridade ?? 1);

                        int rowsAffected = await cmdEvento.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            eventoId = await ObterUltimoEventoId(con, transaction);
                            Debug.WriteLine($"Evento inserido com sucesso! ID: {eventoId}");
                        }

                        // 4. COMMIT DA TRANSAÇÃO
                        await transaction.CommitAsync();

                        TempData["Sucesso"] = $"Evento cadastrado com sucesso! ID: {eventoId}";
                    }
                    catch (Exception ex)
                    {
                        // ROLLBACK em caso de erro
                        await transaction.RollbackAsync();
                        Debug.WriteLine($"ERRO: {ex.Message}");
                        TempData["Erro"] = $"Erro ao cadastrar evento: {ex.Message}";

                        ViewBag.Secretarias = BuscarSecretarias();
                        ViewBag.TiposEvento = BuscarTiposEvento();
                        ViewBag.NiveisPrioridade = BuscarNiveisPrioridade();
                        ViewBag.Bairros = BuscarBairros();
                        return View(model);
                    }
                }
            }

            return RedirectToAction("Criar");
        }

        // Método auxiliar para obter o último ID inserido
        private async Task<int> ObterUltimoEventoId(SqlConnection con, SqlTransaction transaction)
        {
            string sql = "SELECT SCOPE_IDENTITY()";
            SqlCommand cmd = new SqlCommand(sql, con, transaction);
            var result = await cmd.ExecuteScalarAsync();
            return result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }

        // =========================
        // MÉTODOS AUXILIARES PARA BUSCAR DADOS DO BANCO
        // =========================

        private List<SelectListItem> BuscarSecretarias()
        {
            var lista = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Selecione..." }
            };

            try
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    Debug.WriteLine("Connection string está vazia!");
                    return lista;
                }

                using (var con = new SqlConnection(_connectionString))
                {
                    con.Open();

                    string query = "SELECT cdSecretaria, nmSecretaria FROM dbo.Secretaria ORDER BY nmSecretaria";
                    SqlCommand cmd = new SqlCommand(query, con);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new SelectListItem
                            {
                                Value = dr["cdSecretaria"].ToString(),
                                Text = dr["nmSecretaria"].ToString()
                            });
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                Debug.WriteLine($"Erro SQL ao buscar secretarias: {sqlEx.Message}");

                lista.AddRange(new[]
                {
                    new SelectListItem { Value = "1", Text = "Secretaria de Planejamento" },
                    new SelectListItem { Value = "2", Text = "Secretaria de Educação" },
                    new SelectListItem { Value = "3", Text = "Secretaria de Turismo" }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro geral ao buscar secretarias: {ex.Message}");
            }

            return lista;
        }

        private List<SelectListItem> BuscarTiposEvento()
        {
            var lista = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Selecione..." }
            };

            try
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    Debug.WriteLine("Connection string está vazia!");
                    return lista;
                }

                using (var con = new SqlConnection(_connectionString))
                {
                    con.Open();

                    string query = "SELECT cdTipoEvento, nmTipoEvento FROM dbo.TipoEvento ORDER BY nmTipoEvento";
                    SqlCommand cmd = new SqlCommand(query, con);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new SelectListItem
                            {
                                Value = dr["cdTipoEvento"].ToString(),
                                Text = dr["nmTipoEvento"].ToString()
                            });
                        }
                    }
                }
            }
            catch (SqlException)
            {
                lista.AddRange(new[]
                {
                    new SelectListItem { Value = "1", Text = "Evento Cultural" },
                    new SelectListItem { Value = "2", Text = "Palestra" },
                    new SelectListItem { Value = "3", Text = "Workshop" }
                });
            }

            return lista;
        }

        private List<SelectListItem> BuscarNiveisPrioridade()
        {
            var lista = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Selecione..." }
            };

            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    con.Open();

                    string query = "SELECT cdPrioridade, nmPrioridade FROM dbo.NivelPrioridade ORDER BY cdPrioridade";
                    SqlCommand cmd = new SqlCommand(query, con);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new SelectListItem
                            {
                                Value = dr["cdPrioridade"].ToString(),
                                Text = dr["nmPrioridade"].ToString()
                            });
                        }
                    }
                }
            }
            catch (SqlException)
            {
                lista.AddRange(new[]
                {
                    new SelectListItem { Value = "1", Text = "Normal" },
                    new SelectListItem { Value = "2", Text = "Alta" },
                    new SelectListItem { Value = "3", Text = "Inadiável" }
                });
            }

            return lista;
        }

        private List<SelectListItem> BuscarBairros()
        {
            var lista = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Selecione o bairro" }
            };

            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    con.Open();

                    string query = "SELECT DISTINCT dsBairro FROM dbo.Endereco WHERE dsBairro IS NOT NULL AND dsBairro <> '' ORDER BY dsBairro";
                    SqlCommand cmd = new SqlCommand(query, con);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new SelectListItem
                            {
                                Value = dr["dsBairro"].ToString(),
                                Text = dr["dsBairro"].ToString()
                            });
                        }
                    }
                }
            }
            catch
            {
                lista.AddRange(new[]
                {
                    new SelectListItem { Value = "Boqueirão", Text = "Boqueirão" },
                    new SelectListItem { Value = "Aviação", Text = "Aviação" },
                    new SelectListItem { Value = "Caiçara", Text = "Caiçara" },
                    new SelectListItem { Value = "Guilhermina", Text = "Guilhermina" }
                });
            }

            return lista;
        }

        private int ObterUsuarioIdLogado()
        {
            // Implemente conforme seu sistema de autenticação
            // Exemplo:
            // if (User.Identity.IsAuthenticated)
            // {
            //     var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            //     if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            //         return userId;
            // }

            return 1; // Temporário - substitua pela lógica real
        }
    }
}