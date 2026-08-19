using System.Net;
namespace Desafio.Api.Tests;

[Collection(ColecaoDaApi.Nome)]
public class BeneficiariosTests(ApiFixture fixture) : IAsyncLifetime
{
    private HttpClient Client => fixture.Client;

    public Task InitializeAsync() => fixture.LimparAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static object CorpoDeCriacao(string cpf, Guid? planoId = null) => new
    {
        NomeCompleto = "Maria Aparecida da Silva",
        Cpf = cpf,
        DataNascimento = "1990-05-12",
        PlanoId = planoId ?? Planos.Bronze
    };

    // ------------------------------------------------------------------ criação

    [Fact]
    public async Task Criar_deve_devolver_201_com_header_location()
    {
        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(CorpoDeCriacao("52998224725")));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location);

        var corpo = await resposta.CorpoAsync();
        Assert.NotEqual(Guid.Empty, corpo.GetProperty("id").GetGuid());
        Assert.Equal("52998224725", corpo.GetProperty("cpf").GetString());
        Assert.Equal("ATIVO", corpo.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Criar_com_cpf_ja_cadastrado_deve_devolver_409()
    {
        await Client.PostAsync("/beneficiarios", Http.Json(CorpoDeCriacao("71428793860")));

        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(CorpoDeCriacao("71428793860")));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Criar_com_cpf_com_digitos_verificadores_invalidos_deve_devolver_400()
    {
        var corpoComCpfFalso = new
        {
            NomeCompleto = "Usuario Com Cpf Invalido",
            Cpf = "12345678900",
            DataNascimento = "1995-05-10",
            PlanoId = Planos.Bronze
        };

        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(corpoComCpfFalso));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }
    [Fact]
    public async Task Criar_com_data_nascimento_sendo_hoje_deve_devolver_400()
    {
        var corpoRecemNascido = new
        {
            NomeCompleto = "Bebê Recém Nascido",
            Cpf = "52998224725",
            DataNascimento = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            PlanoId = Planos.Bronze
        };

        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(corpoRecemNascido));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("5299822472A")]
    [InlineData("529982247 25")]
    public async Task Criar_com_cpf_fora_do_formato_deve_devolver_400(string cpf)
    {
        var resposta = await Client.PostAsync(
            "/beneficiarios",
            Http.Json(CorpoDeCriacao(cpf)));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Theory]
    [InlineData("00000000000")]
    [InlineData("11111111111")]
    [InlineData("22222222222")]
    [InlineData("33333333333")]
    [InlineData("44444444444")]
    [InlineData("55555555555")]
    [InlineData("66666666666")]
    [InlineData("77777777777")]
    [InlineData("88888888888")]
    [InlineData("99999999999")]
    public async Task Criar_com_cpf_de_digitos_repetidos_deve_devolver_400(string cpf)
    {
        var resposta = await Client.PostAsync(
            "/beneficiarios",
            Http.Json(CorpoDeCriacao(cpf)));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Theory]
    [InlineData("1990")]
    [InlineData("12-05-1990")]
    [InlineData("1990/05/12")]
    [InlineData("abc")]
    public async Task Criar_com_data_nascimento_invalida_deve_devolver_400(string dataNascimento)
    {
        var resposta = await Client.PostAsync(
            "/beneficiarios",
            Http.Json(new
            {
                NomeCompleto = "Maria Aparecida da Silva",
                Cpf = "52998224725",
                DataNascimento = dataNascimento,
                PlanoId = Planos.Bronze
            }));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Excluir_deve_remover_apenas_o_beneficiario_excluido_da_listagem()
    {
        var beneficiarios = await fixture.SemearBeneficiariosAsync(3);

        var excluido = beneficiarios[1];

        var resposta = await Client.DeleteAsync(
            $"/beneficiarios/{excluido.Id}");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);

        var corpo = await (
            await Client.GetAsync("/beneficiarios?tamanho=10")
        ).CorpoAsync();

        Assert.Equal(2, corpo.GetProperty("total").GetInt32());
        Assert.Equal(2, corpo.GetProperty("dados").GetArrayLength());

        var ids = corpo
            .GetProperty("dados")
            .EnumerateArray()
            .Select(x => x.GetProperty("id").GetGuid())
            .ToList();

        Assert.DoesNotContain(excluido.Id, ids);
    }

    [Fact]
    public async Task Criar_com_nome_cpf_e_data_invalidos_deve_devolver_400_com_3_detalhes_de_erro()
    {
        var corpoInvalido = new
        {
            NomeCompleto = "Ab", 
            Cpf = "00000000000", 
            DataNascimento = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"), 
            PlanoId = Planos.Bronze
        };

        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(corpoInvalido));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var corpo = await resposta.CorpoAsync();
        
        // Verifica o tipo do erro e a mensagem
        Assert.Equal("ValidacaoInvalida", corpo.GetProperty("erro").GetString());

        var detalhes = corpo.GetProperty("detalhes");
        Assert.Equal(3, detalhes.GetArrayLength());
    }

    [Fact]
    public async Task Requisicoes_simultaneas_com_mesmo_cpf_devem_cadastrar_apenas_um_beneficiario()
    {
        var cpfDuplicado = "12345678909";
        var quantidadeRequisicoes = 5;

        var tarefas = Enumerable.Range(1, quantidadeRequisicoes).Select(i =>
            Client.PostAsync("/beneficiarios", Http.Json(new
            {
                NomeCompleto = $"Beneficiario Concorrente {i}",
                Cpf = cpfDuplicado,
                DataNascimento = "1990-01-01",
                PlanoId = Planos.Bronze
            }))
        );

        var respostas = await Task.WhenAll(tarefas);

        var sucessos = respostas.Count(r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(1, sucessos);

        var falhas = respostas.Count(r => r.StatusCode == HttpStatusCode.BadRequest 
                                       || r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(quantidadeRequisicoes - 1, falhas);
    }

    [Fact]
    public async Task Criar_com_cpf_diferente_de_11digitos_deve_devolver_400()
    {
        var corpoComCpfFalso = new
        {
            NomeCompleto = "Usuario Com Cpf Invalido",
            Cpf = "1234567",
            DataNascimento = "1995-05-10",
            PlanoId = Planos.Bronze
        };

        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(corpoComCpfFalso));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }
    
    [Fact]
    public async Task Criar_com_data_nascimento_futura_deve_devolver_400()
    {
        var corpoInvalid = new
        {
            NomeCompleto = "Beneficiario do Futuro",
            Cpf = "05322978082",
            DataNascimento = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
            PlanoId = Planos.Bronze
        };

        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(corpoInvalid));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Criar_apontando_para_plano_excluido_deve_devolver_422()
    {
        await Client.DeleteAsync($"/planos/{Planos.Bronze}");

        var resposta = await Client.PostAsync(
            "/beneficiarios", 
            Http.Json(CorpoDeCriacao("06639930404", Planos.Bronze)));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Criar_com_plano_inexistente_deve_devolver_422()
    {
        var resposta = await Client.PostAsync(
            "/beneficiarios",
            Http.Json(CorpoDeCriacao("39053344705", Planos.Inexistente)));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Criar_com_nome_com_menos_de_3_caracteres_deve_devolver_400()
    {
        var corpoInvalido = new
        {
            NomeCompleto = "Ab", 
            Cpf = "05322978082",
            DataNascimento = "1995-05-10",
            PlanoId = Planos.Bronze
        };

        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(corpoInvalido));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Criar_com_nome_com_mais_de_120_caracteres_deve_devolver_400()
    {
        var nomeComMaisDe120Caracteres = new string('A', 121);

        var corpoInvalido = new
        {
            NomeCompleto = nomeComMaisDe120Caracteres,
            Cpf = "05322978082",
            DataNascimento = "1995-05-10",
            PlanoId = Planos.Bronze
        };

        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(corpoInvalido));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }


    // ------------------------------------------------------------------ consulta por id

    [Fact]
    public async Task Obter_deve_devolver_o_beneficiario()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        var resposta = await Client.GetAsync($"/beneficiarios/{beneficiario.Id}");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await resposta.CorpoAsync();
        Assert.Equal(beneficiario.Id, corpo.GetProperty("id").GetGuid());
        Assert.Equal(beneficiario.Cpf, corpo.GetProperty("cpf").GetString());
        Assert.Equal(Planos.Bronze, corpo.GetProperty("plano_id").GetGuid());
    }

    [Fact]
    public async Task Obter_inexistente_deve_devolver_404()
    {
        var resposta = await Client.GetAsync($"/beneficiarios/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    // ------------------------------------------------------------------ atualização

    [Fact]
    public async Task Atualizar_deve_alterar_os_dados_do_beneficiario()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        var resposta = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            NomeCompleto = "Joana Ribeiro Nunes",
            DataNascimento = "1985-03-20",
            PlanoId = Planos.Ouro,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await resposta.CorpoAsync();
        Assert.Equal("Joana Ribeiro Nunes", corpo.GetProperty("nome_completo").GetString());
        Assert.Equal(Planos.Ouro, corpo.GetProperty("plano_id").GetGuid());
    }
    
    [Fact]
    public async Task Atualizar_enviando_novo_cpf_deve_ignorar_cpf_e_manter_o_original()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();
        var cpfOriginal = beneficiario.Cpf;

        var resposta = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            NomeCompleto = "Nome Atualizado",
            Cpf = "99999999999", 
            DataNascimento = "1990-05-12",
            PlanoId = Planos.Bronze,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var consulta = await (await Client.GetAsync($"/beneficiarios/{beneficiario.Id}")).CorpoAsync();
        Assert.Equal(cpfOriginal, consulta.GetProperty("cpf").GetString());
    }

    [Fact]
    public async Task Atualizar_com_status_invalido_deve_devolver_400()
    {
        var beneficiario = (
            await fixture.SemearBeneficiariosAsync(1)
        ).Single();

        var resposta = await Client.PutAsync(
            $"/beneficiarios/{beneficiario.Id}",
            Http.Json(new
            {
                NomeCompleto = beneficiario.NomeCompleto,
                DataNascimento = beneficiario.DataNascimento.ToString("yyyy-MM-dd"),
                PlanoId = beneficiario.PlanoId,
                Status = "QUALQUER_COISA"
            }));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_apontando_para_plano_excluido_deve_devolver_422()
    {
        var beneficiario = (
            await fixture.SemearBeneficiariosAsync(1)
        ).Single();

        await Client.DeleteAsync($"/planos/{Planos.Ouro}");

        var resposta = await Client.PutAsync(
            $"/beneficiarios/{beneficiario.Id}",
            Http.Json(new
            {
                NomeCompleto = beneficiario.NomeCompleto,
                DataNascimento = beneficiario.DataNascimento.ToString("yyyy-MM-dd"),
                PlanoId = Planos.Ouro,
                Status = "ATIVO"
            }));

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            resposta.StatusCode);
    }

    [Fact]
    public async Task Reativar_beneficiario_inativo_e_alterar_outras_informacoes_deve_devolver_200()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(
            1, Planos.Bronze, "INATIVO", 500)).Single();
            
        var resposta = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            NomeCompleto = "Nome Alterado Na Reativacao",
            DataNascimento = beneficiario.DataNascimento.ToString("yyyy-MM-dd"),
            PlanoId = Planos.Ouro,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.CorpoAsync();
        Assert.Equal("ATIVO", corpo.GetProperty("status").GetString());
        Assert.Equal("Nome Alterado Na Reativacao", corpo.GetProperty("nome_completo").GetString());
        Assert.Equal(Planos.Ouro, corpo.GetProperty("plano_id").GetGuid());
    }

    [Fact]
    public async Task Reativar_beneficiario_inativo_mantendo_dados_cadastrais_deve_devolver_200()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(
            1, Planos.Bronze, "INATIVO", 500)).Single();

        var resposta = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            NomeCompleto = beneficiario.NomeCompleto,
            DataNascimento = beneficiario.DataNascimento.ToString("yyyy-MM-dd"),
            PlanoId = beneficiario.PlanoId,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await resposta.CorpoAsync();
        Assert.Equal("ATIVO", corpo.GetProperty("status").GetString());
    }
   

    [Fact]
    public async Task Atualizar_ou_excluir_beneficiario_ja_excluido_deve_devolver_404()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        await Client.DeleteAsync($"/beneficiarios/{beneficiario.Id}");

        var respostaPut = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            NomeCompleto = "Tentativa de Atualizar Deletado",
            DataNascimento = "1990-05-12",
            PlanoId = Planos.Bronze,
            Status = "ATIVO"
        }));
        Assert.Equal(HttpStatusCode.NotFound, respostaPut.StatusCode);

        var respostaDelete = await Client.DeleteAsync($"/beneficiarios/{beneficiario.Id}");
        Assert.Equal(HttpStatusCode.NotFound, respostaDelete.StatusCode);
    }
    
    [Fact]
    public async Task Atualizar_dados_de_beneficiario_inativo_deve_devolver_409()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(
            1, Planos.Bronze, "INATIVO", 500)).Single();

        var resposta = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            NomeCompleto = "Nome Corrigido do Inativo",
            DataNascimento = "1990-05-12",
            PlanoId = Planos.Bronze,
            Status = "INATIVO"
        }));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }
    [Fact]
    public async Task Atualizar_inexistente_deve_devolver_404()
    {
        var resposta = await Client.PutAsync($"/beneficiarios/{Guid.NewGuid()}", Http.Json(new
        {
            NomeCompleto = "Nao Existe",
            DataNascimento = "1985-03-20",
            PlanoId = Planos.Ouro,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_com_nome_com_menos_de_3_caracteres_deve_devolver_400()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        var resposta = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            NomeCompleto = "Ab", 
            DataNascimento = beneficiario.DataNascimento.ToString("yyyy-MM-dd"),
            PlanoId = beneficiario.PlanoId,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_com_nome_com_mais_de_120_caracteres_deve_devolver_400()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();
        var nomeComMaisDe120Caracteres = new string('A', 121);

        var resposta = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            NomeCompleto = nomeComMaisDe120Caracteres,
            DataNascimento = beneficiario.DataNascimento.ToString("yyyy-MM-dd"),
            PlanoId = beneficiario.PlanoId,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_com_data_nascimento_futura_deve_devolver_400()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        var resposta = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            NomeCompleto = beneficiario.NomeCompleto,
            DataNascimento = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"), 
            PlanoId = beneficiario.PlanoId,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_apontando_para_plano_inexistente_deve_devolver_422()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        var resposta = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            NomeCompleto = "Maria Aparecida da Silva",
            DataNascimento = "1990-05-12",
            PlanoId = Planos.Inexistente,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    // ------------------------------------------------------------------ exclusão

    [Fact]
    public async Task Excluir_deve_ser_logico_e_tirar_o_beneficiario_das_consultas()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        var exclusao = await Client.DeleteAsync($"/beneficiarios/{beneficiario.Id}");
        Assert.Equal(HttpStatusCode.NoContent, exclusao.StatusCode);

        var consulta = await Client.GetAsync($"/beneficiarios/{beneficiario.Id}");
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);

        var listagem = await (await Client.GetAsync("/beneficiarios?pagina=1&tamanho=50")).CorpoAsync();
        Assert.Equal(0, listagem.GetProperty("total").GetInt32());

        var novaExclusao = await Client.DeleteAsync($"/beneficiarios/{beneficiario.Id}");
        Assert.Equal(HttpStatusCode.NotFound, novaExclusao.StatusCode);
    }

    [Fact]
    public async Task Cpf_de_beneficiario_excluido_deve_continuar_ocupado()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        await Client.DeleteAsync($"/beneficiarios/{beneficiario.Id}");

        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(CorpoDeCriacao(beneficiario.Cpf)));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    // ------------------------------------------------------------------ listagem

    [Fact]
    public async Task Listar_deve_devolver_envelope_paginado()
    {
        await fixture.SemearBeneficiariosAsync(3);

        var resposta = await Client.GetAsync("/beneficiarios?pagina=1&tamanho=10");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await resposta.CorpoAsync();
        Assert.Equal(3, corpo.GetProperty("dados").GetArrayLength());
        Assert.Equal(1, corpo.GetProperty("pagina").GetInt32());
        Assert.Equal(10, corpo.GetProperty("tamanho").GetInt32());
        Assert.Equal(3, corpo.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Listar_deve_respeitar_pagina_e_tamanho()
    {
        await fixture.SemearBeneficiariosAsync(25);

        var corpo = await (await Client.GetAsync("/beneficiarios?pagina=3&tamanho=10")).CorpoAsync();

        Assert.Equal(5, corpo.GetProperty("dados").GetArrayLength());
        Assert.Equal(3, corpo.GetProperty("pagina").GetInt32());
        Assert.Equal(25, corpo.GetProperty("total").GetInt32());
    }
    [Theory]
    [InlineData("/beneficiarios?pagina=0&tamanho=10")]
    [InlineData("/beneficiarios?pagina=-1&tamanho=10")]
    [InlineData("/beneficiarios?pagina=1&tamanho=0")]
    [InlineData("/beneficiarios?pagina=1&tamanho=101")]
    public async Task Listar_com_parametros_invalidos_deve_devolver_400(string url)
    {
        var resposta = await Client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Listar_paginacao_deve_ser_estavel_sem_repetir_ou_perder_registros()
    {
        await fixture.SemearBeneficiariosAsync(25);

        var ids = new List<Guid>();

        for (var pagina = 1; pagina <= 3; pagina++)
        {
            var corpo = await (
                await Client.GetAsync(
                    $"/beneficiarios?pagina={pagina}&tamanho=10")
            ).CorpoAsync();

            ids.AddRange(
                corpo.GetProperty("dados")
                    .EnumerateArray()
                    .Select(x => x.GetProperty("id").GetGuid()));
        }

        Assert.Equal(25, ids.Count);
        Assert.Equal(25, ids.Distinct().Count());
    }

    [Fact]
    public async Task Listar_deve_combinar_os_filtros_de_status_e_plano()
    {
        await fixture.SemearBeneficiariosAsync(4, Planos.Bronze, "ATIVO", 100);
        await fixture.SemearBeneficiariosAsync(6, Planos.Bronze, "INATIVO", 200);
        await fixture.SemearBeneficiariosAsync(3, Planos.Prata, "ATIVO", 300);

        var corpo = await (await Client.GetAsync(
            $"/beneficiarios?tamanho=50&status=ATIVO&plano_id={Planos.Bronze}")).CorpoAsync();

        Assert.Equal(4, corpo.GetProperty("total").GetInt32());
        Assert.All(
            corpo.GetProperty("dados").EnumerateArray(),
            beneficiario =>
            {
                Assert.Equal("ATIVO", beneficiario.GetProperty("status").GetString());
                Assert.Equal(Planos.Bronze, beneficiario.GetProperty("plano_id").GetGuid());
            });
    }

    [Fact]
    public async Task Listar_sem_informar_tamanho_deve_devolver_10_itens_por_pagina()
    {
        await fixture.SemearBeneficiariosAsync(25);

        var corpo = await (await Client.GetAsync("/beneficiarios")).CorpoAsync();

        Assert.Equal(10, corpo.GetProperty("dados").GetArrayLength());
        Assert.Equal(10, corpo.GetProperty("tamanho").GetInt32());
        Assert.Equal(25, corpo.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Listar_pagina_alem_do_total_existente_deve_devolver_200_com_dados_vazios_e_total_correto()
    {
        await fixture.SemearBeneficiariosAsync(5);

        var resposta = await Client.GetAsync("/beneficiarios?pagina=99&tamanho=10");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await resposta.CorpoAsync();

        Assert.Equal(5, corpo.GetProperty("total").GetInt32());

        var dados = corpo.GetProperty("dados");
        Assert.Equal(0, dados.GetArrayLength());
    }
}
