﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Xml;

using NFe.Components;
using NFe.Settings;
using NFe.Certificado;
using NFSe.Components;
using NFe.Components.SigCorp;
using NFe.Components.Fiorilli;
using NFe.Components.SimplISS;
using NFe.Components.Conam;
using NFe.Components.EGoverne;
using NFe.Components.EL;
using NFe.Components.GovDigital;
using NFe.Components.FISSLEX;
using NFe.Components.Memory;
using NFe.Components.Metropolis;

namespace NFe.Service.NFSe
{
    /// <summary>
    /// Consultar uma NFS-e por RPS
    /// </summary>
    public class TaskNFSeConsultarLoteRps : TaskAbst
    {
        public override void Execute()
        {
            int emp = Empresas.FindEmpresaByThread();

            ///
            /// extensao permitida: PedLoteRps = "-ped-loterps.xml";
            /// 
            /// Definir o serviço que será executado para a classe
            Servico = Servicos.NFSeConsultarLoteRps;

            try
            {
                Functions.DeletarArquivo(Empresas.Configuracoes[emp].PastaXmlRetorno + "\\" +
                                         Functions.ExtrairNomeArq(NomeArquivoXML, Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).EnvioXML) + Propriedade.ExtRetorno.LoteRps_ERR);
                Functions.DeletarArquivo(Empresas.Configuracoes[emp].PastaXmlErro + "\\" + NomeArquivoXML);

                //Ler o XML para pegar parâmetros de envio
                LerXML ler = new LerXML();
                ler.PedSitNfseRps(NomeArquivoXML);
                PadroesNFSe padraoNFSe = Functions.PadraoNFSe(ler.oDadosPedSitNfseRps.cMunicipio);
                //Criar objetos das classes dos serviços dos webservices do SEFAZ
                WebServiceProxy wsProxy = null;
                object pedLoteRps = null;

                if (IsUtilizaCompilacaoWs(padraoNFSe))
                {
                    wsProxy = ConfiguracaoApp.DefinirWS(Servico, emp, ler.oDadosPedSitNfseRps.cMunicipio, ler.oDadosPedSitNfseRps.tpAmb, ler.oDadosPedSitNfseRps.tpEmis, padraoNFSe); ;
                    if (wsProxy != null) pedLoteRps = wsProxy.CriarObjeto(wsProxy.NomeClasseWS);
                }

                System.Net.SecurityProtocolType securityProtocolType = WebServiceProxy.DefinirProtocoloSeguranca(ler.oDadosPedSitNfseRps.cMunicipio, ler.oDadosPedSitNfseRps.tpAmb, ler.oDadosPedSitNfseRps.tpEmis, padraoNFSe, Servico);

                string cabecMsg = "";
                switch (padraoNFSe)
                {
                    case PadroesNFSe.GINFES:
                        if (ler.oDadosPedSitNfseRps.cMunicipio == 4125506) //São José dos Pinhais - PR  
                            cabecMsg = "<ns2:cabecalho versao=\"3\" xmlns:ns2=\"http://nfe.sjp.pr.gov.br/cabecalho_v03.xsd\"><versaoDados>3</versaoDados></ns2:cabecalho>";
                        else
                            cabecMsg = "<ns2:cabecalho versao=\"3\" xmlns:ns2=\"http://www.ginfes.com.br/cabecalho_v03.xsd\"><versaoDados>3</versaoDados></ns2:cabecalho>";
                        break;

                    case PadroesNFSe.BETHA:
                        wsProxy = new WebServiceProxy(Empresas.Configuracoes[emp].X509Certificado);
                        wsProxy.Betha = new Betha();
                        break;

                    case PadroesNFSe.CANOAS_RS:
                        cabecMsg = "<cabecalho versao=\"201001\"><versaoDados>V2010</versaoDados></cabecalho>";
                        break;

                    case PadroesNFSe.BHISS:
                        cabecMsg = "<cabecalho xmlns=\"http://www.abrasf.org.br/nfse.xsd\" versao=\"1.00\"><versaoDados >1.00</versaoDados ></cabecalho>";
                        break;

                    case PadroesNFSe.WEBISS:
                        cabecMsg = "<cabecalho xmlns=\"http://www.abrasf.org.br/nfse.xsd\" versao=\"1.00\"><versaoDados >1.00</versaoDados ></cabecalho>";
                        break;

                    case PadroesNFSe.PORTOVELHENSE:
                        cabecMsg = "<cabecalho versao=\"2.00\" xmlns:ns2=\"http://www.w3.org/2000/09/xmldsig#\" xmlns=\"http://www.abrasf.org.br/nfse.xsd\"><versaoDados>2.00</versaoDados></cabecalho>";
                        break;

                    case PadroesNFSe.TECNOSISTEMAS:
                        cabecMsg = "<?xml version=\"1.0\" encoding=\"utf-8\"?><cabecalho xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" versao=\"20.01\" xmlns=\"http://www.nfse-tecnos.com.br/nfse.xsd\"><versaoDados>20.01</versaoDados></cabecalho>";
                        break;

                    case PadroesNFSe.FINTEL:
                        cabecMsg = "<cabecalho xmlns=\"http://iss.pontagrossa.pr.gov.br/Arquivos/nfse.xsd\" versao=\"1.00\"><versaoDados >1.00</versaoDados ></cabecalho>";
                        break;

                    case PadroesNFSe.SIGCORP_SIGISS:
                        SigCorp sigcorp = new SigCorp((TipoAmbiente)Empresas.Configuracoes[emp].AmbienteCodigo,
                                                        Empresas.Configuracoes[emp].PastaXmlRetorno,
                                                        Convert.ToInt32(ler.oDadosPedSitNfseRps.cMunicipio));
                        sigcorp.ConsultarLoteRps(NomeArquivoXML);
                        break;

                    case PadroesNFSe.FIORILLI:
                        Fiorilli fiorilli = new Fiorilli((TipoAmbiente)Empresas.Configuracoes[emp].AmbienteCodigo,
                                                        Empresas.Configuracoes[emp].PastaXmlRetorno,
                                                        ler.oDadosPedSitNfseRps.cMunicipio,
                                                        Empresas.Configuracoes[emp].UsuarioWS,
                                                        Empresas.Configuracoes[emp].SenhaWS,
                                                        ConfiguracaoApp.ProxyUsuario,
                                                        ConfiguracaoApp.ProxySenha,
                                                        ConfiguracaoApp.ProxyServidor);


                        fiorilli.ConsultarLoteRps(NomeArquivoXML);
                        break;

                    case PadroesNFSe.SIMPLISS:
                        SimplISS simpliss = new SimplISS((TipoAmbiente)Empresas.Configuracoes[emp].AmbienteCodigo,
                                                Empresas.Configuracoes[emp].PastaXmlRetorno,
                                                ler.oDadosPedSitNfseRps.cMunicipio,
                                                Empresas.Configuracoes[emp].UsuarioWS,
                                                Empresas.Configuracoes[emp].SenhaWS,
                                                ConfiguracaoApp.ProxyUsuario,
                                                ConfiguracaoApp.ProxySenha,
                                                ConfiguracaoApp.ProxyServidor);

                        simpliss.ConsultarLoteRps(NomeArquivoXML);
                        break;

                    case PadroesNFSe.CONAM:
                        Conam conam = new Conam((TipoAmbiente)Empresas.Configuracoes[emp].AmbienteCodigo,
                                                Empresas.Configuracoes[emp].PastaXmlRetorno,
                                                ler.oDadosPedSitNfseRps.cMunicipio,
                                                Empresas.Configuracoes[emp].UsuarioWS,
                                                Empresas.Configuracoes[emp].SenhaWS);

                        conam.ConsultarLoteRps(NomeArquivoXML);
                        break;

                    case PadroesNFSe.EGOVERNE:
                        #region E-Governe
                        EGoverne egoverne = new EGoverne((TipoAmbiente)Empresas.Configuracoes[emp].AmbienteCodigo,
                                                        Empresas.Configuracoes[emp].PastaXmlRetorno,
                                                        ler.oDadosPedSitNfseRps.cMunicipio,
                                                        ConfiguracaoApp.ProxyUsuario,
                                                        ConfiguracaoApp.ProxySenha,
                                                        ConfiguracaoApp.ProxyServidor,
                                                        Empresas.Configuracoes[emp].X509Certificado);

                        AssinaturaDigital assegov = new AssinaturaDigital();
                        assegov.Assinar(NomeArquivoXML, emp, ler.oDadosPedSitNfseRps.cMunicipio);

                        egoverne.ConsultarLoteRps(NomeArquivoXML);
                        break;
                        #endregion

                    case PadroesNFSe.EL:
                        #region E&L
                        EL el = new EL((TipoAmbiente)Empresas.Configuracoes[emp].AmbienteCodigo,
                                        Empresas.Configuracoes[emp].PastaXmlRetorno,
                                        ler.oDadosPedSitNfseRps.cMunicipio,
                                        Empresas.Configuracoes[emp].UsuarioWS,
                                        Empresas.Configuracoes[emp].SenhaWS,
                                        (ConfiguracaoApp.Proxy ? ConfiguracaoApp.ProxyUsuario : ""),
                                        (ConfiguracaoApp.Proxy ? ConfiguracaoApp.ProxySenha : ""),
                                        (ConfiguracaoApp.Proxy ? ConfiguracaoApp.ProxyServidor : ""));

                        el.ConsultarLoteRps(NomeArquivoXML);
                        break;
                        #endregion

                    case PadroesNFSe.GOVDIGITAL:
                        GovDigital govdig = new GovDigital((TipoAmbiente)Empresas.Configuracoes[emp].AmbienteCodigo,
                                                            Empresas.Configuracoes[emp].PastaXmlRetorno, 
                                                            Empresas.Configuracoes[emp].X509Certificado,
                                                            ler.oDadosPedSitNfseRps.cMunicipio,
                                                            ConfiguracaoApp.ProxyUsuario,
                                                            ConfiguracaoApp.ProxySenha,
                                                            ConfiguracaoApp.ProxyServidor);

                        AssinaturaDigital adgovdig = new AssinaturaDigital();
                        adgovdig.Assinar(NomeArquivoXML, emp, ler.oDadosPedSitNfseRps.cMunicipio);

                        govdig.ConsultarLoteRps(NomeArquivoXML);
                        break;

                    case PadroesNFSe.EQUIPLANO:
                        cabecMsg = "1";
                        break;

                    case PadroesNFSe.ACTCON:
                    case PadroesNFSe.PRODATA:
                        cabecMsg = "<cabecalho><versaoDados>2.01</versaoDados></cabecalho>";
                        break;

                    case PadroesNFSe.FISSLEX:
                        FISSLEX fisslex = new FISSLEX((TipoAmbiente)Empresas.Configuracoes[emp].AmbienteCodigo,
                                                        Empresas.Configuracoes[emp].PastaXmlRetorno,
                                                        ler.oDadosPedSitNfseRps.cMunicipio,
                                                        Empresas.Configuracoes[emp].UsuarioWS,
                                                        Empresas.Configuracoes[emp].SenhaWS,
                                                        ConfiguracaoApp.ProxyUsuario,
                                                        ConfiguracaoApp.ProxySenha,
                                                        ConfiguracaoApp.ProxyServidor);
                        
                        fisslex.ConsultarLoteRps(NomeArquivoXML);
                        break;

                    case PadroesNFSe.NATALENSE:
                        cabecMsg = "<cabecalho xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" versao=\"1\" xmlns=\"http://www.abrasf.org.br/ABRASF/arquivos/nfse.xsd\"><versaoDados>1</versaoDados></cabecalho>";
                        break;

                    case PadroesNFSe.PAULISTANA:
                        wsProxy = new WebServiceProxy(Empresas.Configuracoes[emp].X509Certificado);

                        if (ler.oDadosPedSitNfseRps.tpAmb == 1)
                        {
                            pedLoteRps = new NFe.Components.PSaoPauloSP.LoteNFe();
                        }
                        else
                        {
                            throw new Exception("Município de São Paulo-SP não dispõe de ambiente de homologação para envio de NFS-e em teste.");
                        }
                        break;

                    case PadroesNFSe.FREIRE_INFORMATICA:
                        cabecMsg = "<cabecalho xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://www.abrasf.org.br/nfse.xsd\" versao=\"2.02\"><versaoDados>2.02</versaoDados></cabecalho>";
                        break;

                    case PadroesNFSe.MEMORY:
                        #region Memory
                        Memory memory = new Memory((TipoAmbiente)Empresas.Configuracoes[emp].AmbienteCodigo,
                        Empresas.Configuracoes[emp].PastaXmlRetorno,
                        ler.oDadosPedSitNfseRps.cMunicipio,
                        Empresas.Configuracoes[emp].UsuarioWS,
                        Empresas.Configuracoes[emp].SenhaWS,
                        ConfiguracaoApp.ProxyUsuario,
                        ConfiguracaoApp.ProxySenha,
                        ConfiguracaoApp.ProxyServidor);

                        memory.CancelarNfse(NomeArquivoXML);
                        break;
                    #endregion

                    case PadroesNFSe.CAMACARI_BA:
                        cabecMsg = "<cabecalho><versaoDados>2.01</versaoDados><versao>2.01</versao></cabecalho>";
                        break;

                    case PadroesNFSe.NA_INFORMATICA:
                        wsProxy = new WebServiceProxy(Empresas.Configuracoes[emp].X509Certificado);

                        if (ler.oDadosPedSitNfseRps.tpAmb == 1)
                            pedLoteRps = new Components.PCorumbaMS.NfseWSService();
                        else
                            pedLoteRps = new Components.HCorumbaMS.NfseWSService();

                        break;

                    case PadroesNFSe.METROPOLIS:
                        #region METROPOLIS
                        Metropolis metropolis = new Metropolis((TipoAmbiente)Empresas.Configuracoes[emp].AmbienteCodigo,
                                                      Empresas.Configuracoes[emp].PastaXmlRetorno,
                                                      ler.oDadosPedSitNfseRps.cMunicipio,
                                                      ConfiguracaoApp.ProxyUsuario,
                                                      ConfiguracaoApp.ProxySenha,
                                                      ConfiguracaoApp.ProxyServidor,
                                                      Empresas.Configuracoes[emp].X509Certificado);

                        AssinaturaDigital metropolisdig = new AssinaturaDigital();
                        metropolisdig.Assinar(NomeArquivoXML, emp, ler.oDadosPedSitNfseRps.cMunicipio);

                        metropolis.ConsultarLoteRps(NomeArquivoXML);
                        break;
                        #endregion
                }

                if (base.IsInvocar(padraoNFSe, Servico))
                {
                    //Assinar o XML
                    AssinaturaDigital ad = new AssinaturaDigital();
                    ad.Assinar(NomeArquivoXML, emp, ler.oDadosPedSitNfseRps.cMunicipio);

                    //Invocar o método que envia o XML para o SEFAZ
                    oInvocarObj.InvocarNFSe(wsProxy, pedLoteRps, NomeMetodoWS(Servico, ler.oDadosPedSitNfseRps.cMunicipio), 
                                            cabecMsg, this,
                                            Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).EnvioXML,   //"-ped-loterps", 
                                            Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).RetornoXML, //"-loterps", 
                                            padraoNFSe, Servico, securityProtocolType);

                    ///
                    /// grava o arquivo no FTP
                    string filenameFTP = Path.Combine(Empresas.Configuracoes[emp].PastaXmlRetorno,
                                                      Functions.ExtrairNomeArq(NomeArquivoXML, Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).EnvioXML) + Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).RetornoXML);
                    if (File.Exists(filenameFTP))
                        new GerarXML(emp).XmlParaFTP(emp, filenameFTP);
                }

            }
            catch (Exception ex)
            {
                try
                {
                    //Gravar o arquivo de erro de retorno para o ERP, caso ocorra
                    TFunctions.GravarArqErroServico(NomeArquivoXML, Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).EnvioXML, Propriedade.ExtRetorno.LoteRps_ERR, ex);
                }
                catch
                {
                    //Se falhou algo na hora de gravar o retorno .ERR (de erro) para o ERP, infelizmente não posso fazer mais nada.
                    //Wandrey 31/08/2011
                }
            }
            finally
            {
                try
                {
                    Functions.DeletarArquivo(NomeArquivoXML);
                }
                catch
                {
                    //Se falhou algo na hora de deletar o XML de cancelamento de NFe, infelizmente
                    //não posso fazer mais nada, o UniNFe vai tentar mandar o arquivo novamente para o webservice, pois ainda não foi excluido.
                    //Wandrey 31/08/2011
                }
            }
        }
    }
}
