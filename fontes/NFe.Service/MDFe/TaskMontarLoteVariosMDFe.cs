﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Xml;
using NFe.Components;
using NFe.Settings;
using NFe.Certificado;
using NFe.Exceptions;

namespace NFe.Service
{
    /// <summary>
    /// Classe responsável por montar lote de vários MDFes
    /// </summary>
    public class TaskMDFeMontarLoteVarias : TaskAbst
    {
        public TaskMDFeMontarLoteVarias()
        {
            Servico = Servicos.MDFeMontarLoteVarios;
            oGerarXML.Servico = Servico;
        }

        public override void Execute()
        {
            int emp = Empresas.FindEmpresaByThread();
            List<string> arquivosNFe = new List<string>();

            //Aguardar a assinatura de todos os arquivos da pasta de lotes
            arquivosNFe = oAux.ArquivosPasta(Empresas.Configuracoes[emp].PastaXmlEmLote, "*" + Propriedade.Extensao(Propriedade.TipoEnvio.MDFe).EnvioXML);
            if (arquivosNFe.Count == 0)
            {
                List<string> notas = new List<string>();
                FileStream fsArquivo = null;
                FluxoNfe fluxoNfe = new FluxoNfe();

                try
                {
                    try
                    {
                        XmlDocument doc = new XmlDocument(); //Criar instância do XmlDocument Class
                        fsArquivo = new FileStream(NomeArquivoXML, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); //Abrir um arquivo XML usando FileStream
                        doc.Load(fsArquivo); //Carregar o arquivo aberto no XmlDocument

                        string versaoXml = string.Empty;

                        XmlNodeList documentoList = doc.GetElementsByTagName("MontarLoteMDFe"); //Pesquisar o elemento Documento no arquivo XML
                        foreach (XmlNode documentoNode in documentoList)
                        {
                            XmlElement documentoElemento = (XmlElement)documentoNode;

                            int QtdeArquivo = documentoElemento.GetElementsByTagName("ArquivoMDFe").Count;

                            for (int d = 0; d < QtdeArquivo; d++)
                            {
                                string arquivoNFe = Empresas.Configuracoes[emp].PastaXmlEmLote + Propriedade.NomePastaXMLAssinado + "\\" + documentoElemento.GetElementsByTagName("ArquivoMDFe")[d].InnerText;

                                if (File.Exists(arquivoNFe))
                                {
                                    DadosNFeClass oDadosNfe = this.LerXMLNFe(arquivoNFe);
                                    if (string.IsNullOrEmpty(versaoXml))
                                        versaoXml = oDadosNfe.versao;

                                    if (!fluxoNfe.NFeComLote(oDadosNfe.chavenfe))
                                    {
                                        notas.Add(arquivoNFe);
                                    }
                                    else
                                    {
                                        throw new Exception("Arquivo: " + arquivoNFe + " já está no fluxo de envio e não será incluido em novo lote.");
                                    }
                                }
                                else
                                {
                                    throw new Exception("Arquivo: " + arquivoNFe + " não existe e não será incluido no lote!");
                                }
                            }
                        }

                        fsArquivo.Close(); //Fecha o arquivo XML

                        this.LoteNfe(notas, versaoXml);
                    }
                    catch
                    {
                        if (fsArquivo != null)
                        {
                            fsArquivo.Close();
                        }
                    }

                    //Deletar o arquivo de solicitão de montagem do lote de NFe
                    FileInfo oArquivo = new FileInfo(this.NomeArquivoXML);
                    oArquivo.Delete();
                }
                catch (Exception ex)
                {
                    try
                    {
                        TFunctions.GravarArqErroServico(this.NomeArquivoXML, Propriedade.Extensao(Propriedade.TipoEnvio.MontarLote).EnvioXML, Propriedade.ExtRetorno.MontarLote_ERR, ex);
                    }
                    catch
                    {
                        //Se deu algum erro na hora de gravar o arquivo de erro de retorno para o ERP, infelizmente não poderemos fazer nada
                        //pois deve estar ocorrendo alguma falha de rede, hd, permissão de acesso a pasta ou arquivos, etc. Wandrey 22/03/2010
                        //TODO: Não poderia gravar algum LOG para análise? Erro de rede normalmente é erro de IO
                    }
                }
            }

        }
    }
}