using IntegracoesVETX.Entity;
using IntegracoesVETX.Util;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegracoesVETX.DAL
{
    public class BusinessPartnersDAL
    {

        private SAPbobsCOM.Company oCompany;

        private Log log;

        internal BusinessPartnersDAL()
         {
            this.log = new Log();
            //this.oCompany = company;
        }

        public void InserirBusinessPartner(SAPbobsCOM.Company company, Cliente cliente, Endereco endereco, Pedido pedido, out string messageError)
        {
            int addBPNumber = 0;

            string document = string.Empty;
            Boolean isCorporate = false;
            Boolean marketPlace = false;

            if (pedido.origin.Equals("Fulfillment"))
            {
                marketPlace = true;
            }

            if (marketPlace)
            {
                document = pedido.clientProfileData.document;
            }

            if (cliente.isCorporate != null && cliente.isCorporate.Equals("true"))
            {
                document = cliente.corporateDocument;
                isCorporate = true;
            }
            else if (cliente.isCorporate != null && cliente.isCorporate.Equals("false"))
            {
                document = cliente.document;
            }

            try
            {
                CountyDAL countyDAL = new CountyDAL();

                this.oCompany = company;

                int _groupCode = Convert.ToInt32(ConfigurationManager.AppSettings["GroupCode"]);
                int _splCode = Convert.ToInt32(ConfigurationManager.AppSettings["SlpCode"]);
                int _QoP = Convert.ToInt32(ConfigurationManager.AppSettings["QoP"]);
                int groupNum = Convert.ToInt32(ConfigurationManager.AppSettings["GroupNum"]);
                string indicadorIE = ConfigurationManager.AppSettings["IndicadorIE"];
                string indicadorOpConsumidor = ConfigurationManager.AppSettings["IndicadorOpConsumidor"];
                string gerente = ConfigurationManager.AppSettings["Gerente"];
                int priceList = Convert.ToInt32(ConfigurationManager.AppSettings["PriceList"]);
                string cardCodePrefix = ConfigurationManager.AppSettings["CardCodePrefix"];
                int categoriaCliente = Convert.ToInt32(ConfigurationManager.AppSettings["CategoriaCliente"]);
                
                Log.WriteLogCliente("Inserindo Cliente " + cardCodePrefix + document);

                BusinessPartners oBusinessPartner = null;
                oBusinessPartner = (SAPbobsCOM.BusinessPartners)oCompany.GetBusinessObject(BoObjectTypes.oBusinessPartners);

                BusinessPartners oBusinessPartnerUpdateTest = null;
                oBusinessPartnerUpdateTest = (SAPbobsCOM.BusinessPartners)oCompany.GetBusinessObject(BoObjectTypes.oBusinessPartners);

                if (oBusinessPartnerUpdateTest.GetByKey(cardCodePrefix + document))
                {
                    oBusinessPartner = oBusinessPartnerUpdateTest;
                }

                //Setando campos padrões
                oBusinessPartner.CardCode = cardCodePrefix + document;

                if (marketPlace)
                {
                    oBusinessPartner.CardName = pedido.clientProfileData.firstName + " " + pedido.clientProfileData.lastName;
                    //oBusinessPartner.EmailAddress = cliente.email;
                }
                else {
                    oBusinessPartner.CardName = cliente.firstName + " " + cliente.lastName;
                    oBusinessPartner.EmailAddress = cliente.email;
                }
                
                oBusinessPartner.CardType = BoCardTypes.cCustomer;
                oBusinessPartner.GroupCode = _groupCode;
                oBusinessPartner.SalesPersonCode = _splCode;
                oBusinessPartner.PayTermsGrpCode = groupNum;
                oBusinessPartner.PriceListNum = priceList;
                //oBusinessPartner.CardForeignName = "Teste";

                //Setando campos de usuário
                oBusinessPartner.UserFields.Fields.Item("U_TX_IndIEDest").Value = indicadorIE;
                oBusinessPartner.UserFields.Fields.Item("U_TX_IndFinal").Value = indicadorOpConsumidor;
                oBusinessPartner.UserFields.Fields.Item("U_Gerente").Value = gerente;
                oBusinessPartner.UserFields.Fields.Item("U_CategoriaCliente").Value = gerente;


                //removendo o +55
                if (cliente.homePhone != null)
                {
                    oBusinessPartner.Phone1 = cliente.homePhone.Substring(2);
                }

                if (cliente.phone != null)
                {
                    oBusinessPartner.Cellular = cliente.phone.Substring(2);
                }

                if (marketPlace)
                {
                    oBusinessPartner.Phone1 = pedido.clientProfileData.phone;
                }

                string codMunicipio = string.Empty;

                if (!marketPlace)
                {
                    codMunicipio = countyDAL.RecuperarCodigoMunicipio(endereco.city, this.oCompany);
                }
                else {
                    if (pedido.shippingData.address.city != null)
                    {
                        codMunicipio = countyDAL.RecuperarCodigoMunicipio(pedido.shippingData.address.city, this.oCompany);
                    }
                }

                //Adicionando endereços
                //Cobrança
                oBusinessPartner.Addresses.SetCurrentLine(0);
                oBusinessPartner.Addresses.AddressType = BoAddressType.bo_BillTo;
                oBusinessPartner.Addresses.AddressName = "COBRANCA";

                if (marketPlace)
                {
                    oBusinessPartner.Addresses.City = pedido.shippingData.address.city;
                }
                else
                {
                    oBusinessPartner.Addresses.City = endereco.city;
                }

                if (marketPlace && pedido.shippingData.address.complement != null && pedido.shippingData.address.complement.Length <= 100)
                {
                    oBusinessPartner.Addresses.BuildingFloorRoom = pedido.shippingData.address.complement;
                }
                else
                {
                    if (endereco != null && endereco.complement != null && endereco.complement.Length <= 100)
                    {
                        oBusinessPartner.Addresses.BuildingFloorRoom = endereco.complement;
                    }
                }

                if (marketPlace)
                {
                    //oBusinessPartner.Addresses.Country = "1058";
                    oBusinessPartner.Addresses.Block = pedido.shippingData.address.neighborhood;
                    oBusinessPartner.Addresses.StreetNo = pedido.shippingData.address.number;
                    oBusinessPartner.Addresses.ZipCode = pedido.shippingData.address.postalCode;
                    oBusinessPartner.Addresses.State = pedido.shippingData.address.state;
                    oBusinessPartner.Addresses.Street = pedido.shippingData.address.street;
                    oBusinessPartner.Addresses.County = codMunicipio;
                    //oBusinessPartner.Addresses.Country = "br";
                }
                else
                {
                    //oBusinessPartner.Addresses.Country = "1058";
                    oBusinessPartner.Addresses.Block = endereco.neighborhood;
                    oBusinessPartner.Addresses.StreetNo = endereco.number;
                    oBusinessPartner.Addresses.ZipCode = endereco.postalCode;
                    oBusinessPartner.Addresses.State = endereco.state;
                    oBusinessPartner.Addresses.Street = endereco.street;
                    oBusinessPartner.Addresses.County = codMunicipio;
                    //oBusinessPartner.Addresses.Country = "br";
                }
                oBusinessPartner.Addresses.Add();

                //FATURAMENTO
                oBusinessPartner.Addresses.SetCurrentLine(1);
                oBusinessPartner.Addresses.AddressType = BoAddressType.bo_ShipTo;
                oBusinessPartner.Addresses.AddressName = "FATURAMENTO";

                if (marketPlace)
                {
                    oBusinessPartner.Addresses.City = pedido.shippingData.address.city;
                }
                else
                {
                    oBusinessPartner.Addresses.City = endereco.city;
                }

                if (marketPlace && pedido.shippingData.address.complement != null && pedido.shippingData.address.complement.Length <= 100)
                {
                    oBusinessPartner.Addresses.BuildingFloorRoom = pedido.shippingData.address.complement;
                }
                else
                {
                    if (endereco != null && endereco.complement != null && endereco.complement.Length <= 100)
                    {
                        oBusinessPartner.Addresses.BuildingFloorRoom = endereco.complement;
                    }
                }

                if (marketPlace)
                {
                    //oBusinessPartner.Addresses.Country = "1058";
                    oBusinessPartner.Addresses.Block = pedido.shippingData.address.neighborhood;
                    oBusinessPartner.Addresses.StreetNo = pedido.shippingData.address.number;
                    oBusinessPartner.Addresses.ZipCode = pedido.shippingData.address.postalCode;
                    oBusinessPartner.Addresses.State = pedido.shippingData.address.state;
                    oBusinessPartner.Addresses.Street = pedido.shippingData.address.street;
                    oBusinessPartner.Addresses.County = codMunicipio;
                    //oBusinessPartner.Addresses.Country = "br";
                }
                else
                {
                    //oBusinessPartner.Addresses.Country = "1058";
                    oBusinessPartner.Addresses.Block = endereco.neighborhood;
                    oBusinessPartner.Addresses.StreetNo = endereco.number;
                    oBusinessPartner.Addresses.ZipCode = endereco.postalCode;
                    oBusinessPartner.Addresses.State = endereco.state;
                    oBusinessPartner.Addresses.Street = endereco.street;
                    oBusinessPartner.Addresses.County = codMunicipio;
                    //oBusinessPartner.Addresses.Country = "br";
                }
                oBusinessPartner.Addresses.Add();

                #region ENDEREÇO FOR
                /*
                for (int i = 0; i < 2; i++)
                {
                    if (i > 0)
                    {
                        oBusinessPartner.Addresses.SetCurrentLine(i);
                        oBusinessPartner.Addresses.AddressType = BoAddressType.bo_ShipTo;
                        oBusinessPartner.Addresses.AddressName = "FATURAMENTO";
                    }
                    else
                    {
                        oBusinessPartner.Addresses.SetCurrentLine(i);
                        oBusinessPartner.Addresses.AddressType = BoAddressType.bo_BillTo;
                        oBusinessPartner.Addresses.AddressName = "COBRANCA";

                        if (!oBusinessPartnerUpdateTest.GetByKey(cardCodePrefix + document))
                        {
                            oBusinessPartner.Addresses.Add();
                        }
                    }

                    if (marketPlace)
                    {
                        oBusinessPartner.Addresses.City = pedido.shippingData.address.city;
                    }
                    else {
                        oBusinessPartner.Addresses.City = endereco.city;
                    }

                    if (marketPlace && pedido.shippingData.address.complement != null && pedido.shippingData.address.complement.Length <= 100)
                    {
                        oBusinessPartner.Addresses.BuildingFloorRoom = pedido.shippingData.address.complement;
                    }
                    else {
                        if (endereco!= null && endereco.complement != null && endereco.complement.Length <= 100)
                        {
                            oBusinessPartner.Addresses.BuildingFloorRoom = endereco.complement;
                        }
                    }

                    if (marketPlace)
                    {
                        //oBusinessPartner.Addresses.Country = "1058";
                        oBusinessPartner.Addresses.Block = pedido.shippingData.address.neighborhood;
                        oBusinessPartner.Addresses.StreetNo = pedido.shippingData.address.number;
                        oBusinessPartner.Addresses.ZipCode = pedido.shippingData.address.postalCode;
                        oBusinessPartner.Addresses.State = pedido.shippingData.address.state;
                        oBusinessPartner.Addresses.Street = pedido.shippingData.address.street;
                        oBusinessPartner.Addresses.County = codMunicipio;
                        //oBusinessPartner.Addresses.Country = "br";
                    }
                    else {
                        //oBusinessPartner.Addresses.Country = "1058";
                        oBusinessPartner.Addresses.Block = endereco.neighborhood;
                        oBusinessPartner.Addresses.StreetNo = endereco.number;
                        oBusinessPartner.Addresses.ZipCode = endereco.postalCode;
                        oBusinessPartner.Addresses.State = endereco.state;
                        oBusinessPartner.Addresses.Street = endereco.street;
                        oBusinessPartner.Addresses.County = codMunicipio;
                        //oBusinessPartner.Addresses.Country = "br";
                    }

                }*/
                #endregion

                #region código de endereço antigo
                /*oBusinessPartner.Addresses.SetCurrentLine(0);
                oBusinessPartner.Addresses.AddressName = "COBRANCA";
                oBusinessPartner.Addresses.AddressType = SAPbobsCOM.BoAddressType.bo_BillTo;

                oBusinessPartner.Addresses.Street = endereco.street;
                oBusinessPartner.Addresses.Block = endereco.neighborhood;
                oBusinessPartner.Addresses.ZipCode = endereco.postalCode;
                oBusinessPartner.Addresses.City = endereco.city;
                oBusinessPartner.Addresses.Country = "BR";
                oBusinessPartner.Addresses.State = endereco.state;
                oBusinessPartner.Addresses.BuildingFloorRoom = endereco.complement;
                oBusinessPartner.Addresses.StreetNo = endereco.number;
                
                oBusinessPartner.Addresses.Add();

                oBusinessPartner.Addresses.SetCurrentLine(1);
                oBusinessPartner.Addresses.AddressName = "FATURAMENTO";
                oBusinessPartner.Addresses.AddressType = SAPbobsCOM.BoAddressType.bo_ShipTo;

                oBusinessPartner.Addresses.Street = endereco.street;
                oBusinessPartner.Addresses.Block = endereco.neighborhood;
                oBusinessPartner.Addresses.ZipCode = endereco.postalCode;
                oBusinessPartner.Addresses.City = endereco.city;
                oBusinessPartner.Addresses.Country = "BR";
                oBusinessPartner.Addresses.State = endereco.state;
                oBusinessPartner.Addresses.BuildingFloorRoom = endereco.complement;
                oBusinessPartner.Addresses.StreetNo = endereco.number;*/
                //oBusinessPartner.Addresses.Add();
                #endregion

                oBusinessPartner.BilltoDefault = "COBRANCA";
                oBusinessPartner.ShipToDefault = "FATURAMENTO";

                BusinessPartners oBusinessPartnerUpdate = null;
                oBusinessPartnerUpdate = (SAPbobsCOM.BusinessPartners)oCompany.GetBusinessObject(BoObjectTypes.oBusinessPartners);

                if (oBusinessPartnerUpdate.GetByKey(cardCodePrefix + document))
                {
                    addBPNumber = oBusinessPartner.Update();

                    if (addBPNumber != 0)
                    {
                        messageError = oCompany.GetLastErrorDescription();
                        this.log.WriteLogTable(oCompany, EnumTipoIntegracao.Cliente, document, cardCodePrefix+document, EnumStatusIntegracao.Erro, messageError);
                        //this.log.WriteLogCliente("InserirBusinessPartner error SAP: " + messageError);
                    }
                    else
                    {
                        messageError = "";
                        this.log.WriteLogTable(oCompany,EnumTipoIntegracao.Cliente,document, cardCodePrefix + document,EnumStatusIntegracao.Sucesso,"Cliente atualizado com sucesso.");
                        //this.Log.WriteLogCliente("BusinessPartner " + cardCodePrefix + document + " atualizado com sucesso.");

                        System.Runtime.InteropServices.Marshal.ReleaseComObject(oBusinessPartner);
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(oBusinessPartnerUpdate);
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(oBusinessPartnerUpdateTest);
                    }

                }
                else
                {
                    //Setando informações Fiscais
                    //oBusinessPartner.FiscalTaxID.SetCurrentLine(0);
                    if (isCorporate)
                    {
                        oBusinessPartner.FiscalTaxID.TaxId0 = document;
                    }
                    else {

                        oBusinessPartner.FiscalTaxID.TaxId4 = document;
                        oBusinessPartner.FiscalTaxID.TaxId1 = "Isento";
                    }
                    //oBusinessPartner.FiscalTaxID.Address = "FATURAMENTO";
                    //oBusinessPartner.FiscalTaxID.Add();

                    addBPNumber = oBusinessPartner.Add();

                    if (addBPNumber != 0)
                    {
                        messageError = oCompany.GetLastErrorDescription();
                        this.log.WriteLogTable(oCompany, EnumTipoIntegracao.Cliente, document, "", EnumStatusIntegracao.Erro, messageError);
                        //Log.WriteLogCliente("InserirBusinessPartner error SAP: " + messageError);
                    }
                    else
                    {
                        string CardCode = oCompany.GetNewObjectKey();
                        this.log.WriteLogTable(oCompany, EnumTipoIntegracao.Cliente, document, CardCode, EnumStatusIntegracao.Sucesso, "Cliente inserido com sucesso.");
                        //Log.WriteLogCliente("BusinessPartner " + cardCodePrefix +CardCode + " inserido com sucesso.");
                        messageError = "";
                    }
                }
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oBusinessPartner);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oBusinessPartnerUpdateTest);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oBusinessPartnerUpdate);
            }
            catch (Exception e)
            {
                this.log.WriteLogTable(oCompany, EnumTipoIntegracao.Cliente, document, "", EnumStatusIntegracao.Erro, e.Message);
                Log.WriteLogCliente("InserirBusinessPartner Exception: " + e.Message);
                throw;
            }

        }

    }
}
