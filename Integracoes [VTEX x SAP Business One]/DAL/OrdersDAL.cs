using IntegracoesVETX.Entity;
using IntegracoesVETX.Util;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegracoesVETX.DAL
{
    public class OrdersDAL
    {
        private SAPbobsCOM.Company oCompany;

        private Log log;
        internal OrdersDAL(SAPbobsCOM.Company company) {
            this.oCompany = company;
        }

        public int InsertOrder(Pedido pedido, out string messageError) {
            this.log = new Log();
            try
            {
                int oOrderNum = 0;

                log.WriteLogPedido("Inserindo Pedido de Venda");

                SAPbobsCOM.Documents oOrder = (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);

                int filial = Convert.ToInt32(ConfigurationManager.AppSettings["Empresa"]);
                string usage = ConfigurationManager.AppSettings["Usage"];
                string WhsCode = ConfigurationManager.AppSettings["WhsCode"];
                int SlpCode = Convert.ToInt32(ConfigurationManager.AppSettings["SlpCode"]);
                string comments = ConfigurationManager.AppSettings["Comments"];
                string plataforma = ConfigurationManager.AppSettings["Plataforma"];
                string carrier = ConfigurationManager.AppSettings["Carrier"];
                string packDesc = ConfigurationManager.AppSettings["PackDesc"];
                int qoP = Convert.ToInt32(ConfigurationManager.AppSettings["QoP"]);
                int expnsCode = Convert.ToInt32(ConfigurationManager.AppSettings["ExpnsCode"]);
                string expnsTax = ConfigurationManager.AppSettings["ExpnsTax"];
                string cardCodePrefix = ConfigurationManager.AppSettings["CardCodePrefix"];
                string pickRemark = ConfigurationManager.AppSettings["PickRemark"];

                oOrder.BPL_IDAssignedToInvoice = filial;
                oOrder.NumAtCard = pedido.orderId;
                oOrder.SalesPersonCode = SlpCode;
                oOrder.Comments = comments;
                oOrder.UserFields.Fields.Item("U_PLATF").Value = plataforma;
                oOrder.UserFields.Fields.Item("U_NumPedEXT").Value = pedido.orderId;
                oOrder.TaxExtension.Carrier = carrier;
                oOrder.TaxExtension.PackDescription = packDesc;
                oOrder.TaxExtension.PackQuantity = qoP;
                oOrder.Expenses.ExpenseCode = expnsCode;
                oOrder.Expenses.TaxCode = expnsTax;

                if (!string.IsNullOrEmpty(pedido.clientProfileData.document))
                {
                    if (!string.IsNullOrEmpty(pedido.clientProfileData.corporateDocument))
                    {
                        oOrder.CardCode = cardCodePrefix + pedido.clientProfileData.corporateDocument;
                    }
                    else {
                        oOrder.CardCode = cardCodePrefix + pedido.clientProfileData.document;
                    }
                    
                    //Log.WriteLogPedido("Verificando documento do Cliente (CardCode) "+ cardCodePrefix + pedido.clientProfileData.document);
                    //oOrder.CardCode = "E22268140865";
                }

                if (pedido.shippingData.logisticsInfo.Length > 0)
                {
                    foreach(Logisticsinfo logInfo in pedido.shippingData.logisticsInfo) {
                        oOrder.DocDueDate = DateTime.Parse(logInfo.shippingEstimateDate);

                        //PickRemark - Recuperando tipo de Frete
                        if (!string.IsNullOrEmpty(logInfo.deliveryCompany))
                        {
                            oOrder.PickRemark = logInfo.deliveryCompany;
                        }
                    }
                }

                double _valorFrete = 0.00;
                double _valorDescont = 0.00;
                double _valorTaxa = 0.00;

                //despesas adicionais
                if (pedido.totals.Length > 0)
                {
                    foreach (Total total in pedido.totals)
                    {
                        if (total.id.Equals("Discounts"))
                        {
                            if (total.value != 0)
                            {
                                _valorDescont = Convert.ToDouble(total.value.ToString().Insert(total.value.ToString().Length - 2, ","));
                            }
                        }
                        if (total.id.Equals("Shipping"))
                        {
                            if (total.value != 0)
                            {
                                _valorFrete = Convert.ToDouble(total.value.ToString().Insert(total.value.ToString().Length - 2, ","));
                            }
                        }
                        if (total.id.Equals("Tax"))
                        {
                            if (total.value != 0)
                            {
                                _valorTaxa = Convert.ToDouble(total.value.ToString().Insert(total.value.ToString().Length - 2, ","));
                            }
                        }
                    }
                }

                oOrder.Expenses.LineGross = _valorFrete;

                //DocumentLines
                if (pedido.items.Length > 0)
                {
                    //_valorFrete.ToString().Insert(1,".");
                    int _lineNum = 0;

                    foreach (ItemVtex item in pedido.items)
                    {
                        if (item.refId != null)
                        {
                            oOrder.Lines.ItemCode = item.refId;
                            oOrder.Lines.Quantity = item.quantity;
                            oOrder.Lines.WarehouseCode = WhsCode;
                            oOrder.Lines.Usage = usage;
                            oOrder.Lines.SetCurrentLine(_lineNum);
                            oOrder.Lines.Add();
                        }

                        _lineNum++;
                    }
                }

                oOrderNum = oOrder.Add();

                if (oOrderNum != 0)
                {
                    messageError = oCompany.GetLastErrorDescription();
                    log.WriteLogTable(oCompany, EnumTipoIntegracao.PedidoVenda, pedido.orderId, "", EnumStatusIntegracao.Erro, messageError);
                    log.WriteLogPedido("InsertOrder error SAP: " + messageError);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oOrder);
                    return oOrderNum;
                }
                else
                {
                    messageError = "";
                    string docNum = oCompany.GetNewObjectKey();
                    log.WriteLogTable(oCompany, EnumTipoIntegracao.PedidoVenda, pedido.orderId, docNum, EnumStatusIntegracao.Sucesso, "Pedido de venda inserido com sucesso.");
                    log.WriteLogPedido("Pedido de venda inserido com sucesso.");
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oOrder);
                    return oOrderNum;
                }


            }
            catch (Exception e)
            {
                log.WriteLogTable(oCompany, EnumTipoIntegracao.PedidoVenda, pedido.orderId, "", EnumStatusIntegracao.Erro, e.Message);
                log.WriteLogPedido("Excpetion InsertOrder. "+e.Message);

                throw;
            }
        }

        public SAPbobsCOM.Recordset RecuperarNumeroNF()
        {
            string _query = string.Empty;

            //string whsCode = ConfigurationManager.AppSettings["WhsCode"];
            SAPbobsCOM.Recordset recordSet = (SAPbobsCOM.Recordset)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            try
            {
                //this.oCompany = CommonConn.InitializeCompany();

                if (this.oCompany.Connected)
                {
                    recordSet = (SAPbobsCOM.Recordset)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

                    _query = string.Format("SELECT " +
                            "T0.DocNum AS docNPV " +
                            ",T0.NumAtCard AS idOrderVtex " +
                            ", T0.U_NumPedEXT AS idOrderVtex2 " +
                            ",T2.DocEntry AS externalId " +
                            ",T2.DocNum	AS docSAP " +
                            ",T2.Serial AS invoiceNumber " +
                            ",T2.DocDate AS invoiceDate " +
                            ",T3.KeyNfe AS nfeKey " +
                            ",T0.PickRmrk AS shippingMethod " +
                            ",T2.SeriesStr AS invoiceOrderSeries " +
                            ",T1.ItemCode AS codItem " +
                            ",T1.Price AS precoItem " +
                            ",T1.Quantity AS qtdItem " +
                            ",T0.DocTotal AS totalNF " +
                            "FROM    ORDR T0 " +
                            "INNER JOIN INV1 T1 ON T0.DocEntry = T1.BaseEntry  " +
                            "INNER JOIN OINV T2 ON T1.DocEntry = T2.DocEntry and T0.BPLId = T2.BPLId  " +
                            "INNER JOIN [DBInvOne].[dbo].[Process] T3 on T3.DocEntry = T2.DocEntry " +
                            "WHERE	T0.U_PLATF = '{0}' " +
                            "AND    T2.U_EnvioNFVTEX IS NULL " +
                            "ORDER BY docNPV desc ", ConfigurationManager.AppSettings["Plataforma"]);

                    recordSet.DoQuery(_query);

                    //Log.WriteLog("Query: "+_query);

                    if (recordSet.RecordCount > 0)
                    {

                        return recordSet;
                    }
                }

                //CommonConn.FinalizeCompany();

            }
            catch (Exception e)
            {
                this.log = new Log();
                this.log.WriteLogEstoque("Exception recuperarSaldoEstoqueSAP " + e.Message);
                throw;
            }

            return recordSet;
        }

        public int AtualizarPedidoVenda(SAPbobsCOM.Company company, int docEntry) {
            this.log = new Log();
            try
            {
                this.oCompany = company;

                log.WriteLogPedido("Atualizando Pedido de Venda - NF enviada p/ VTEX");

                SAPbobsCOM.Documents oInvoice = (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInvoices);

                if (oInvoice.GetByKey(docEntry))
                {
                    //SAPbobsCOM.Documents oOrderUpdate = (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
                    //oOrderUpdate = oOrder;

                    oInvoice.UserFields.Fields.Item("U_EnvioNFVTEX").Value = "S";

                    int updateOrderNum = oInvoice.Update();

                    if (updateOrderNum != 0)
                    {
                        string messageError = oCompany.GetLastErrorDescription();
                        log.WriteLogPedido("AtualizarPedidoVenda error SAP: " + messageError);
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(oInvoice);
                        return 1;
                    }
                    else
                    {
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(oInvoice);
                        return 0;
                    }
                }
                return 1;
            }
            catch (Exception)
            {
                return 1;
                throw;
            }
        }
    }
}
